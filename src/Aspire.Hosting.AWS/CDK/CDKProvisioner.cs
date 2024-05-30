// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Amazon.CDK;
using Amazon.CDK.CXAPI;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS.CloudFormation;
using Constructs;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.AWS.CDK;

internal sealed class CDKProvisioner(
    DistributedApplicationModel appModel,
    ResourceNotificationService notificationService,
    ResourceLoggerService loggerService) : CloudFormationProvisioner(appModel, notificationService, loggerService)
{

    private const string AppResourceTypeName = "CDK";

    private Lazy<IImmutableDictionary<IStackResource, IEnumerable<IConstructResource>>> ConstructResourcesInStack { get; } = new(appModel.Resources.GetResourcesGroupedByParent<IStackResource, IConstructResource>());

    protected override async Task ProcessCloudFormationResourcesAsync(IEnumerable<CloudFormationResource> resources, CancellationToken cancellationToken = default)
    {
        var cloudFormationResources = resources as CloudFormationResource[] ?? resources.ToArray();
        await base.ProcessCloudFormationResourcesAsync(cloudFormationResources, cancellationToken).ConfigureAwait(false);

        PrepareConstructResources(AppModel.Resources.OfType<IResourceWithConstruct>().ToArray());
        await ProcessAppResourcesAsync(AppModel.Resources.OfType<AppResource>().ToArray(), cancellationToken).ConfigureAwait(false);
    }

    private async Task ProcessAppResourcesAsync(AppResource[] appResources, CancellationToken cancellationToken = default)
    {
        foreach (var appResource in appResources)
        {
            await PublishUpdateStateAsync(appResource, Constants.ResourceStateStarting).ConfigureAwait(false);
        }
        // Process App Resources
        foreach (var appResource in appResources)
        {
            await ProcessAppResourceAsync(appResource, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ProcessAppResourceAsync(AppResource appResource, CancellationToken cancellationToken = default)
    {
        var logger = LoggerService.GetLogger(appResource);
        var stackResources = appResource.ListChildren(AppModel.Resources.OfType<StackResource>()).ToDictionary(x => x.Stack.StackName);
        try
        {
            var cloudAssembly = appResource.App.Synth();
            var templates = cloudAssembly.Stacks.Select(stack => new CDKStackTemplate(stack, stackResources[stack.StackName])).ToArray();

            // Deploy Stack assets
            foreach (var template in templates)
            {
                await DeployCDKStackAssetsAsync(template).ConfigureAwait(false);
            }
            // Deploy Stack templates
            foreach (var template in templates)
            {
                if (await DeployCDKStackTemplatesAsync(template, cancellationToken).ConfigureAwait(false))
                {
                    continue;
                }

                await PublishUpdateStateAsync(appResource, Constants.ResourceStateFailedToStart).ConfigureAwait(false);
                logger.LogError("Error provisioning {ResourceName} template", template.Resource.Name);
                return;
            }
            await PublishUpdateStateAsync(appResource, Constants.ResourceStateRunning).ConfigureAwait(false);
            logger.LogInformation("CDK app provisioning complete");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error synthesizing {ResourceName} CDK resource", appResource.Name);
            await PublishUpdateStateAsync(appResource, Constants.ResourceStateFailedToStart).ConfigureAwait(false);
            foreach (var stackResource in stackResources.Values)
            {
                await PublishUpdateStateAsync(stackResource, Constants.ResourceStateFailedToStart).ConfigureAwait(false);
                stackResource.ProvisioningTaskCompletionSource?.TrySetException(ex);
            }
        }
    }

    private Task DeployCDKStackAssetsAsync(CDKStackTemplate template)
    {
        var logger = LoggerService.GetLogger(template.Resource);
        if (template.Artifact.Dependencies
            .OfType<AssetManifestArtifact>()
            .Any(dependency =>
                dependency.Contents.Files?.Count > 1
                || dependency.Contents.DockerImages?.Count > 0))
        {
            logger.LogError("File or container image assets are currently not supported");
            throw new AWSProvisioningException("Failed to provision stack assets. Provisioning file or container image assets are currently not supported.");
        }

        return Task.CompletedTask;
    }

    private async Task<bool> DeployCDKStackTemplatesAsync(CDKStackTemplate template, CancellationToken cancellationToken = default)
    {
        var logger = LoggerService.GetLogger(template.Resource);
        try
        {
            await PublishUpdateStateAsync(template.Resource, Constants.ResourceStateStarting).ConfigureAwait(false);

            using var cfClient = GetCloudFormationClient(template.Resource);

            var executor = new CloudFormationStackExecutor(cfClient, template, logger);
            var stack = await executor.ExecuteTemplateAsync(cancellationToken).ConfigureAwait(false);

            if (stack != null)
            {
                logger.LogInformation("CDK stack has {Count} output parameters", stack.Outputs.Count);
                if (logger.IsEnabled(LogLevel.Information))
                {
                    foreach (var output in stack.Outputs)
                    {
                        logger.LogInformation("Output Name: {Name}, Value {Value}", output.OutputKey, output.OutputValue);
                    }
                }

                logger.LogInformation("CDK provisioning complete");

                template.Resource.Outputs = stack.Outputs;
                await PublishUpdateStateAsync(template.Resource, Constants.ResourceStateRunning, ConvertOutputToProperties(stack, template.Artifact.TemplateFullPath)).ConfigureAwait(false);
                template.Resource.ProvisioningTaskCompletionSource?.TrySetResult();
                return true;
            }
            logger.LogError("CDK provisioning failed");

            await PublishUpdateStateAsync(template.Resource, Constants.ResourceStateFailedToStart).ConfigureAwait(false);
            template.Resource.ProvisioningTaskCompletionSource?.TrySetException(new AWSProvisioningException("Failed to apply CloudFormation template"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error provisioning {ResourceName} CDK resource", template.Resource.Name);
            await PublishUpdateStateAsync(template.Resource, Constants.ResourceStateFailedToStart).ConfigureAwait(false);
            template.Resource.ProvisioningTaskCompletionSource?.TrySetException(ex);
        }
        return false;
    }

    private static void PrepareConstructResources(IResourceWithConstruct[] constructResources)
    {
        // Modified constructs after build
        foreach (var constructResource in constructResources)
        {
            // Find Construct Modifier Annotations
            if (!constructResource.TryGetAnnotationsOfType<IConstructModifierAnnotation>(out var modifiers))
            {
                continue;
            }

            // Modify stack
            foreach (var modifier in modifiers)
            {
                modifier.ChangeConstruct(constructResource.Construct);
            }
        }
    }

    private async Task PublishUpdateStateAsync(IAppResource resource, string status, ImmutableArray<ResourcePropertySnapshot>? properties = null)
    {
        if (properties == null)
        {
            properties = ImmutableArray.Create<ResourcePropertySnapshot>();
        }

        await NotificationService.PublishUpdateAsync(resource, state => state with
        {
            ResourceType = AppResourceTypeName,
            State = status,
            Properties = state.Properties.AddRange(properties)
        }).ConfigureAwait(false);
    }

    private async Task PublishUpdateStateAsync(IStackResource resource, string status, ImmutableArray<ResourcePropertySnapshot>? properties = null)
    {
        if (properties == null)
        {
            properties = ImmutableArray.Create<ResourcePropertySnapshot>();
        }

        await NotificationService.PublishUpdateAsync(resource, state => state with
        {
            ResourceType = GetResourceType<Stack>(resource),
            State = status,
            Properties = state.Properties.AddRange(properties)
        }).ConfigureAwait(false);
        if (ConstructResourcesInStack.Value.TryGetValue(resource, out var constructResources))
        {
            foreach (var constructResource in constructResources)
            {
                await NotificationService
                    .PublishUpdateAsync(constructResource,
                        state => state with { ResourceType = GetResourceType<Construct>(constructResource), State = status })
                    .ConfigureAwait(false);
            }
        }
    }

    private static string GetResourceType<T>(IResourceWithConstruct constructResource)
        where T : Construct
    {
        var constructType = constructResource.Construct.GetType();
        var baseConstructType = typeof(T);
        return constructType == baseConstructType ? baseConstructType.Name : constructType.Name;
    }
}

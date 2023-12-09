// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS.CDK.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ApplicationModel_StackResource = Aspire.Hosting.AWS.CDK.ApplicationModel.StackResource;

namespace Aspire.Hosting.AWS.CDK.Provisioning;

internal sealed class AWSCDKProvisioner(
    IOptions<AWSCDKProvisionerOptions> options,
    IOptions<PublishingOptions> publishingOptions,
    ILogger<AWSCDKProvisioner> logger) : IDistributedApplicationLifecycleHook
{
    private readonly AWSCDKProvisionerOptions _options = options.Value;

    private const string SHA256_TAG = "AspireAppHost_SHA256";
    private const string IN_PROGRESS_SUFFIX = "IN_PROGRESS";

    public async Task BeforeStartAsync(DistributedApplicationModel model, CancellationToken cancellationToken = default)
    {
        if (publishingOptions.Value.Publisher is "manifest" or "cdk")
        {
            return;
        }
        var resources = model.Resources.OfType<IConstructResource>();
        if (!resources.Any())
        {
            return;
        }

        try
        {
            var builder = new AWSCDKApplicationBuilder();
            var stack = builder.AddStack(new ApplicationModel_StackResource(_options.StackName));
            ProvisionConstructResources(resources, stack);
            ModifyConstructResources(resources);
            var outputs = await ProvisionCloudFormation(builder.Build(), cancellationToken).ConfigureAwait(false);
            ProcessStackOutputs(outputs, resources);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error provisioning AWS resources");
        }
    }

    private void ProvisionConstructResources(IEnumerable<IConstructResource> resources, AWSCDKStackBuilder builder)
    {
        foreach (var resource in resources)
        {
            logger.LogInformation("Building construct {resource}", resource.Name);
            if (resource is IConstructBuilder resourceBuilder)
            {
                builder.AddConstruct(resourceBuilder);
            }
        }
    }

    private void ModifyConstructResources(IEnumerable<IConstructResource> resources)
    {
        foreach (var resource in resources)
        {
            ModifyConstructResource(resource);
        }
    }

    private void ModifyConstructResource(IConstructResource resource)
    {
        if (!resource.TryGetAnnotationsOfType<IConstructModifierAnnotation>(out var modifiers))
        {
            return;
        }

        foreach (var modifier in modifiers)
        {
            var construct = resource.GetConstruct();
            if (construct == null)
            {
                continue;
            }
            logger.LogInformation("Modifying construct resource {resource}", resource.Name);
            modifier.ChangeConstruct(construct, resource);
        }
    }

    private void ProcessStackOutputs(IEnumerable<Output> outputs, IEnumerable<IConstructResource> resources)
    {
        foreach (var output in outputs)
        {
            var exportNameSegments = output.ExportName.Split(':');
            if (exportNameSegments.Length != 3)
            {
                continue;
            }
            var resource = resources.FirstOrDefault(x => x.Name == exportNameSegments[1]);
            if (resource == null)
            {
                continue;
            }

            logger.LogInformation("Processing output {outputKey}", output.OutputKey);
            resource.Outputs.Add(exportNameSegments[2], output.OutputValue);
        }
    }

    private async Task<IEnumerable<Output>> ProvisionCloudFormation(AWSCDKApplication application, CancellationToken cancellationToken)
    {
        var stack = application.Assembly.GetStackByName(_options.StackName);
        logger.LogInformation("Provisioning CloudFormation stack {StackName}", stack.StackName);
        var client = new AmazonCloudFormationClient();
        var templateBody = JsonSerializer.Serialize(stack.Template);
        var templateSha256 = ComputeSHA256(templateBody);
        var cfStack = await FindExistingStackAsync(client, stack.StackName).ConfigureAwait(false);
        if (cfStack == null)
        {
            var createStackRequest = new CreateStackRequest
            {
                StackName = stack.StackName,
                TemplateBody = templateBody,
                Tags = {new Tag { Key = SHA256_TAG, Value = templateSha256} }
            };
            logger.LogInformation("Create CloudFormation stack {StackName}", stack.StackName);
            try
            {
                await client.CreateStackAsync(createStackRequest, cancellationToken).ConfigureAwait(false);
            }
            catch(AmazonCloudFormationException ex)
            {
                logger.LogError(ex, "Error creating CloudFormation stack {StackName}", stack.StackName);
                throw new AWSProvisioningException($"Error creating CloudFormation stack {stack.StackName}", ex);
            }
            cfStack = await WaitStackToCompleteAsync(client, stack.StackName, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var tags = cfStack.Tags;
            var shaTag = tags.FirstOrDefault(x => string.Equals(x.Key, SHA256_TAG, StringComparison.Ordinal));
            if (string.Equals(templateSha256, shaTag?.Value, StringComparison.Ordinal))
            {
                logger.LogInformation("CloudFormation Template for CloudFormation stack {StackName} has not changed", stack.StackName);
            }
            else
            {
                // Update the CloudFormation tag with the latest SHA256.
                if (shaTag != null)
                {
                    shaTag.Value = templateSha256;
                }
                else
                {
                    tags.Add(new Tag { Key = SHA256_TAG, Value = templateSha256 });
                }

                var updateStackRequest = new UpdateStackRequest
                {
                    StackName = stack.StackName,
                    TemplateBody = templateBody,
                    Tags = tags
                };

                logger.LogInformation("Updating CloudFormation stack {StackName}", stack.StackName);
                try
                {
                    await client.UpdateStackAsync(updateStackRequest, cancellationToken).ConfigureAwait(false);
                }
                catch (AmazonCloudFormationException ex)
                {
                    logger.LogError(ex, "Error updating CloudFormation stack {StackName}", stack.StackName);
                    throw new AWSProvisioningException($"Error updating CloudFormation stack {stack.StackName}", ex);
                }
                cfStack = await WaitStackToCompleteAsync(client, stack.StackName, cancellationToken).ConfigureAwait(false);
            }
        }
        logger.LogDebug("CloudFormation stack has {Count} output parameters", cfStack.Outputs.Count);
        foreach(var output in cfStack.Outputs)
        {
            logger.LogDebug("Output Name: {Name}, Value {Value}", output.OutputKey, output.OutputValue);
        }

        return cfStack.Outputs;
    }

    private async Task<Stack> WaitStackToCompleteAsync(IAmazonCloudFormation cfClient, string stackName, CancellationToken cancellationToken)
    {
        const int TIMESTAMP_WIDTH = 20;
        const int LOGICAL_RESOURCE_WIDTH = 40;
        const int RESOURCE_STATUS = 40;
        var mostRecentEventId = string.Empty;

        var minTimeStampForEvents = DateTime.Now;
        logger.LogInformation("Waiting for CloudFormation stack {StackName} to be ready", stackName);

        Stack stack;
        do
        {
            await Task.Delay(_options.StackPollingDelay, cancellationToken).ConfigureAwait(false);

            // If we are in the WaitStackToCompleteAsync then we already know the stack exists.
            stack = (await FindExistingStackAsync(cfClient, stackName).ConfigureAwait(false))!;

            var events = await GetLatestEventsAsync(cfClient, stackName, minTimeStampForEvents, mostRecentEventId, cancellationToken).ConfigureAwait(false);
            if (events.Count > 0)
            {
                mostRecentEventId = events[0].EventId;
            }
            for (var i = events.Count - 1; i >= 0; i--)
            {
                var line =
                    events[i].Timestamp.ToString("g", CultureInfo.InvariantCulture).PadRight(TIMESTAMP_WIDTH) + " " +
                    events[i].LogicalResourceId.PadRight(LOGICAL_RESOURCE_WIDTH) + " " +
                    events[i].ResourceStatus.ToString(CultureInfo.InvariantCulture).PadRight(RESOURCE_STATUS);

                if (!events[i].ResourceStatus.ToString(CultureInfo.InvariantCulture).EndsWith(IN_PROGRESS_SUFFIX) && !string.IsNullOrEmpty(events[i].ResourceStatusReason))
                {
                    line += " " + events[i].ResourceStatusReason;
                }

                if(minTimeStampForEvents < events[i].Timestamp)
                {
                    minTimeStampForEvents = events[i].Timestamp;
                }

                logger.LogInformation(line);
            }

        } while (stack.StackStatus.ToString(CultureInfo.InvariantCulture).EndsWith(IN_PROGRESS_SUFFIX));

        return stack;
    }

    private static async Task<List<StackEvent>> GetLatestEventsAsync(IAmazonCloudFormation cfClient, string stackName, DateTime minTimeStampForEvents, string mostRecentEventId, CancellationToken cancellationToken)
    {
        var noNewEvents = false;
        var events = new List<StackEvent>();
        DescribeStackEventsResponse? response = null;
        do
        {
            var request = new DescribeStackEventsRequest() { StackName = stackName };
            if (response != null)
            {
                request.NextToken = response.NextToken;
            }

            try
            {
                response = await cfClient.DescribeStackEventsAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                throw new AWSProvisioningException($"Error getting events for CloudFormation stack: {e.Message}", e);
            }
            foreach (var @event in response.StackEvents)
            {
                if (string.Equals(@event.EventId, mostRecentEventId) || @event.Timestamp < minTimeStampForEvents)
                {
                    noNewEvents = true;
                    break;
                }

                events.Add(@event);
            }

        } while (!noNewEvents && !string.IsNullOrEmpty(response.NextToken));

        return events;
    }

    private static async Task<Stack?> FindExistingStackAsync(IAmazonCloudFormation client, string stackName)
    {
        await foreach(var stack in client.Paginators.DescribeStacks(new DescribeStacksRequest()).Stacks)
        {
            if(string.Equals(stackName, stack.StackName, StringComparison.Ordinal))
            {
                return stack;
            }
        }
        return null;
    }

    private static string ComputeSHA256(string templateBody)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(templateBody));
        var builder = new StringBuilder();
        foreach (var t in bytes)
        {
            builder.Append(t.ToString("x2", CultureInfo.InvariantCulture));
        }
        return builder.ToString();
    }
}

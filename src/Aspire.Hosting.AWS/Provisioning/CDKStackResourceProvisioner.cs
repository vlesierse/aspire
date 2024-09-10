// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CloudFormation;
using Amazon.Runtime;
using Amazon.S3;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS.CDK;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.AWS.Provisioning;

internal class CDKStackResourceProvisioner(
    ResourceLoggerService loggerService,
    ResourceNotificationService notificationService)
    : CloudFormationTemplateResourceProvisioner<StackResource>(loggerService, notificationService)
{
    protected override async Task GetOrCreateResourceAsync(StackResource resource, CancellationToken cancellationToken)
    {
        var logger = LoggerService.GetLogger(resource);
        await ProvisionStackAssetsAsync(resource, logger).ConfigureAwait(false);
        await base.GetOrCreateResourceAsync(resource, cancellationToken).ConfigureAwait(false);
    }

    private static Task ProvisionStackAssetsAsync(StackResource resource, ILogger logger)
    {
        // Currently CDK Stack Assets like S3 and Container images are not supported. When a stack contains those assets
        // we stop provisioning as it can introduce unwanted issues.
        if (!resource.TryGetAssetsArtifact(out var artifact))
        {
            return Task.CompletedTask;
        }

        var fileAssetPublisher = new CDKFileAssetPublisher(GetS3Client(resource), logger);
        var imageAssetPublisher = new CDKImageAssetPublisher(logger);
        var tasks = artifact.Contents.Files?.Select(fileAsset => fileAssetPublisher.Publish(fileAsset.Key, fileAsset.Value)) ?? [];
        tasks = tasks.Concat(artifact.Contents.DockerImages?.Select(imageAsset => imageAssetPublisher.Publish(imageAsset.Key, imageAsset.Value)) ?? []);

        return Task.WhenAll(tasks);
    }

    protected override void HandleTemplateProvisioningException(Exception ex, StackResource resource, ILogger logger)
    {
        if (ex.InnerException is AmazonCloudFormationException inner && inner.Message.StartsWith(@"Unable to fetch parameters [/cdk-bootstrap/"))
        {
            logger.LogError("The environment doesn't have the CDK toolkit stack installed. Use 'cdk boostrap' to setup your environment for use AWS CDK with Aspire");
        }
    }

    protected static IAmazonS3 GetS3Client(IStackResource resource)
    {
        if (resource.S3Client != null)
        {
            return resource.S3Client;
        }

        try
        {
            AmazonS3Client client;
            if (resource.AWSSDKConfig != null)
            {
                var config = resource.AWSSDKConfig.CreateServiceConfig<AmazonS3Config>();

                var awsCredentials = FallbackCredentialsFactory.GetCredentials(config);
                client = new AmazonS3Client(awsCredentials, config);
            }
            else
            {
                client = new AmazonS3Client();
            }

            client.BeforeRequestEvent += SdkUtilities.ConfigureUserAgentString;

            return client;
        }
        catch (Exception e)
        {
            throw new AWSProvisioningException("Failed to construct AWS S3 service client to provision AWS resources.", e);
        }
    }
}

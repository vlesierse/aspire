// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CDK.CloudAssemblySchema;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.AWS.Provisioning;

internal class CDKImageAssetPublisher(ILogger logger)
{
    public Task Publish(string id, IDockerImageAsset asset)
    {
        logger.LogInformation("Publishing file asset {Id} to {BucketName}", id, asset.Destinations.First().Key);
        return Task.CompletedTask;
    }
}

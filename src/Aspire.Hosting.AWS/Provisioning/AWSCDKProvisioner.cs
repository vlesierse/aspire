// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.AWS.Provisioning;

internal sealed class AWSCDKProvisioner(
    IOptions<AWSProvisionerOptions> options,
    IOptions<PublishingOptions> publishingOptions,
    ILogger<AWSCDKProvisioner> logger) : IDistributedApplicationLifecycleHook
{
    private readonly AWSProvisionerOptions _options = options.Value;

    public async Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        if (publishingOptions.Value.Publisher == "manifest")
        {
            return;
        }
        var resources = appModel.Resources.OfType<IAWSResource>();
        if (!resources.Any())
        {
            return;
        }

        try
        {
            await ProvisionAWSResources(resources).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error provisioning AWS resources.");
        }
    }

    private Task ProvisionAWSResources(IEnumerable<IAWSResource> resources)
    {
        foreach (var resource in resources)
        {
            logger.LogInformation("Provisioning {resource} in {stack}", resource.Name, _options.StackName);
        }

        return Task.CompletedTask;
    }
}

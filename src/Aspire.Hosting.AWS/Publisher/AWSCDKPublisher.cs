// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS.ApplicationModel;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.AWS.Publisher;

public class AWSCDKPublisher(
    ILogger<AWSCDKPublisher> logger,
    IOptions<AWSCDKPublishingOptions> options,
    IHostApplicationLifetime lifetime) : IDistributedApplicationPublisher
{
    private readonly AWSCDKPublishingOptions _options = options.Value;

    public Task PublishAsync(DistributedApplicationModel model, CancellationToken cancellationToken)
    {
        logger.LogInformation("Publishing to AWS CDK...");
        var resources = model.Resources.OfType<IConstructResource>();
        if (!resources.Any())
        {
            return Task.CompletedTask;
        }
        var builder = new AWSCDKApplicationBuilder();
        var stack = builder.AddStack(new StackResource(_options.StackName));
        PublishConstructResources(resources, stack);
        ModifyConstructResources(resources);
        lifetime.StopApplication();
        return Task.CompletedTask;
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

    private void PublishConstructResources(IEnumerable<IConstructResource> resources, AWSCDKStackBuilder builder)
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
}

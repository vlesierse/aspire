// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS.ApplicationModel;
using Aspire.Hosting.Publishing;
using Constructs;
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
        lifetime.StopApplication();
        return Task.CompletedTask;
    }

    private void PublishConstructResources(IEnumerable<IConstructResource> resources, AWSCDKStackBuilder builder)
    {
        foreach (var resource in resources)
        {
            logger.LogInformation("Building construct {resource}", resource.Name);
            builder.AddConstruct((IConstructResource<Construct>)resource);
        }
    }
}

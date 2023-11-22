// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CDK;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;
using Constructs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using IResource = Aspire.Hosting.ApplicationModel.IResource;

namespace Aspire.Hosting.AWS.Publisher;

public class AWSCDKPublisher(ILogger<AWSCDKPublisher> logger, IHostApplicationLifetime lifetime) : IDistributedApplicationPublisher
{
    private readonly ILogger<AWSCDKPublisher> _logger = logger;
    private readonly IHostApplicationLifetime _lifetime = lifetime;

    public async Task PublishAsync(DistributedApplicationModel model, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Publishing to AWS CDK...");
        await PublishInternalAsync(model, cancellationToken).ConfigureAwait(false);
        _lifetime.StopApplication();
    }

    protected virtual Task PublishInternalAsync(DistributedApplicationModel model, CancellationToken cancellationToken)
    {
        var app = new App(new AppProps { Outdir = Path.Combine(System.Environment.CurrentDirectory, "cdk.out") });
        var stack = new Stack(app, "Aspire");
        WriteResources(model, stack);
        var output = app.Synth();
        _logger.LogInformation("Published cdk to: {outputPath}", output.Directory);
        return Task.CompletedTask;
    }

    private void WriteResources(DistributedApplicationModel model, Construct scope)
    {
        foreach (var resource in model.Resources)
        {
            WriteResource(resource, scope);
        }
    }

    private void WriteResource(IResource resource, Construct scope)
    {
        if (resource.TryGetLastAnnotation<AWSCDKPublishingAnnotationCallback>(out var publishingCallbackAnnotation))
        {
            if (publishingCallbackAnnotation.Callback != null)
            {
                 var construct = publishingCallbackAnnotation.Callback(scope, resource.Name);
                _logger.LogInformation("Published {resourceName} to {constructName}", resource.Name, construct.Node.Path);
            }
        }
    }
 }

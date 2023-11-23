// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.AWS.Publisher;

public class AWSCDKPublisher(ILogger<AWSCDKPublisher> logger, IHostApplicationLifetime lifetime) : IDistributedApplicationPublisher
{
    private readonly ILogger<AWSCDKPublisher> _logger = logger;
    private readonly IHostApplicationLifetime _lifetime = lifetime;

    public Task PublishAsync(DistributedApplicationModel model, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Publishing to AWS CDK...");
        _lifetime.StopApplication();
        return Task.CompletedTask;
    }
}

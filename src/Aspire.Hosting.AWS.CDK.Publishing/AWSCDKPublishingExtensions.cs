// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.AWS.CDK.Publisher;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting;

public static class AWSCDKPublishingExtensions
{
    public static IDistributedApplicationBuilder AddAWSCDKPublishing(this IDistributedApplicationBuilder builder)
    {
        builder.Services.AddKeyedSingleton<IDistributedApplicationPublisher, AWSCDKPublisher>("cdk");
        return builder;
    }
}

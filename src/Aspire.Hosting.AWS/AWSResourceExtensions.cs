// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS;
using Aspire.Hosting.AWS.Publisher;
using Aspire.Hosting.Publishing;
using Constructs;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting;

public static class AWSResourceExtensions
{
    public static IDistributedApplicationBuilder AddAWSCDKPublisher(this IDistributedApplicationBuilder builder)
    {
        builder.Services.AddKeyedSingleton<IDistributedApplicationPublisher, ManifestPublisher>("cdk");
        return builder;
    }

    public static IResourceBuilder<AWSCDKResource> AddAWSCDKResource(this IDistributedApplicationBuilder builder, string name, Func<Construct, string, Construct> factory)
    {
        return builder
            .AddResource(new AWSCDKResource(name))
            .WithAnnotation(new AWSCDKPublishingAnnotationCallback(factory));
        //.WithAnnotation(new ManifestPublishingCallbackAnnotation(WriteAzureKeyVaultToManifest));
    }
}

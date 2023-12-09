// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CDK;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS.CDK.ApplicationModel;
using Aspire.Hosting.AWS.CDK.Provisioning;
using Aspire.Hosting.AWS.CDK.Publisher;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Publishing;
using Constructs;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting;

public static class AWSCDKResourceExtensions
{
    public const string DefaultConfigSection = "AWS::RESOURCES";
    public static IDistributedApplicationBuilder AddAWSCDKPublisher(this IDistributedApplicationBuilder builder)
    {
        builder.Services.AddKeyedSingleton<IDistributedApplicationPublisher, AWSCDKPublisher>("cdk");
        return builder;
    }

    public static IDistributedApplicationBuilder AddAWSProvisioning(this IDistributedApplicationBuilder builder)
    {
        builder.Services.AddLifecycleHook<AWSCDKProvisioner>();
        _ = builder.Services.AddOptions<AWSCDKProvisionerOptions>().BindConfiguration("AWS");
        return builder;
    }

    public static IResourceBuilder<IStackResource> AddStack<T>(this IDistributedApplicationBuilder builder, string name)
        where T : Stack
    {
        return builder.AddResource(new StackResource(name));
    }

    public static IResourceBuilder<IConstructResource<T>> AddConstruct<T>(this IDistributedApplicationBuilder builder,
        string name, ConstructBuilderDelegate<T> constructBuilder)
        where T : Construct
    {
        return builder.AddResource(new ConstructResource<T>(name, constructBuilder));
    }

    public static IResourceBuilder<IConstructResource<T>> WithOutput<T>(this IResourceBuilder<IConstructResource<T>> builder,
        string name, ConstructOutputDelegate<T> output)
        where T : Construct
    {
        return builder.WithAnnotation(new ConstructOutputAnnotation<T>(name, output));
    }

    public static IResourceBuilder<TDestination> WithReference<TDestination, TConstruct>(
        this IResourceBuilder<TDestination> builder,
        IResourceBuilder<IConstructResource<TConstruct>> constructBuilder, string configSection = DefaultConfigSection)
        where TDestination : IResourceWithEnvironment
        where TConstruct: Construct
    {
        configSection = configSection.Replace(':', '_');
        return builder.WithEnvironment(context =>
        {
            if (context.PublisherName == "manifest" || constructBuilder.Resource.Construct == null)
            {
                return;
            }
            foreach(var output in constructBuilder.Resource.Outputs)
            {
                var envName = $"{configSection}__{output.Key}";
                context.EnvironmentVariables[envName] = output.Value;
            }
        });
    }
}

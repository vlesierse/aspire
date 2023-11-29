// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CDK;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS.ApplicationModel;
using Aspire.Hosting.AWS.Provisioning;
using Aspire.Hosting.AWS.Publisher;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Publishing;
using Constructs;
using Microsoft.Extensions.DependencyInjection;
using IResource = Aspire.Hosting.ApplicationModel.IResource;

namespace Aspire.Hosting;

public static class AWSCDKResourceExtensions
{
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
        string name, BuildConstructDelegate<T> build)
        where T : Construct
    {
        return builder
            .AddResource(new ConstructResource<T>(name, build));
    }

    public static IResourceBuilder<TResource> WithReference<TResource, TConstruct>(
        this IResourceBuilder<TResource> builder,
        IResourceBuilder<IConstructResource<TConstruct>> constructBuilder,
        Func<TConstruct, string> resolver)
        where TResource : IResource
        where TConstruct: Construct
    {
        return builder;
    }
}

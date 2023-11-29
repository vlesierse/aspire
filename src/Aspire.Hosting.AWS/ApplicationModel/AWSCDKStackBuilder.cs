// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CDK;
using Constructs;

namespace Aspire.Hosting.AWS.ApplicationModel;

public class AWSCDKStackBuilder(Stack stack)
{
    public Stack Stack { get; } = stack;

    public AWSCDKStackBuilder AddConstruct(IConstructResource<Construct> resource)
    {
        resource.Build(Stack, resource.Name);
        return this;
    }
}

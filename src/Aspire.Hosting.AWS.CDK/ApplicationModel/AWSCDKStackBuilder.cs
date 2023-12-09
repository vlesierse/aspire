// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CDK;
using Constructs;

namespace Aspire.Hosting.AWS.CDK.ApplicationModel;

public class AWSCDKStackBuilder(Stack<> stack)
{
    public Stack<> Stack { get; } = stack;

    public Construct AddConstruct(IConstructBuilder builder)
    {
        var construct = builder.BuildConstruct(Stack);
        return construct;
    }
}

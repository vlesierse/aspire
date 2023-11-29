// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CDK;

namespace Aspire.Hosting.AWS.ApplicationModel;

public class AWSCDKApplicationBuilder
{
    public App App { get; } = new App();

    public AWSCDKStackBuilder AddStack(IStackResource resource)
    {
        var stack = new Stack(App, resource.Name);
        return new AWSCDKStackBuilder(stack);
    }

    public AWSCDKApplication Build()
    {
        return new AWSCDKApplication(App.Synth());
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CDK;
using Constructs;

namespace Aspire.Hosting.AWS.CDK.ApplicationModel;

public class ConstructOutputAnnotation<T>(string name, ConstructOutputDelegate<T> output)
    : IConstructModifierAnnotation
    where T : Construct
{
    public string Name { get; } = name;

    public string? Description { get; set; }

    public void ChangeConstruct(Construct construct, IConstructResource resource)
    {
        var target = (T)construct;
        var export = $"{Stack<>.Of(construct).StackName}:{resource.Name}:{Name}";
        _ = new CfnOutput(construct, Name, new CfnOutputProps
        {
            Value = output(target),
            Description = Description,
            ExportName = export
        });
    }
}

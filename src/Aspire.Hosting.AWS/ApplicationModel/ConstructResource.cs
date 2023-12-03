// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Constructs;

namespace Aspire.Hosting.AWS.ApplicationModel;

public class ConstructResource<T>(string name, ConstructBuilderDelegate<T> builder)
    : Resource(name), IConstructResource<T>, IConstructBuilder
    where T : Construct
{
    public T? Construct { get; private set; }

    public Construct BuildConstruct(Construct scope)
    {
        Construct = builder(scope, Name);
        return Construct;
    }

    public Construct? GetConstruct() => Construct;

    public IDictionary<string, string> Outputs { get; } = new Dictionary<string, string>();
}

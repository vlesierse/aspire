// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Constructs;

namespace Aspire.Hosting.AWS.ApplicationModel;

public class ConstructReference<T>(string name, T construct) : Resource(name), IConstructResource<T>
    where T : Construct
{
    public T? Construct { get; } = construct;

    public IDictionary<string, string> Outputs { get; } = new Dictionary<string, string>();

    public Construct? GetConstruct() => Construct;
}

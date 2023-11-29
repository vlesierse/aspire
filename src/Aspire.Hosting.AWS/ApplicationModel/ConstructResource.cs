// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Constructs;

namespace Aspire.Hosting.AWS.ApplicationModel;

public class ConstructResource<T>(string name, BuildConstructDelegate<T> build) : Resource(name), IConstructResource<T>
    where T : Construct
{
    public BuildConstructDelegate<T> Build { get; } = build;
}

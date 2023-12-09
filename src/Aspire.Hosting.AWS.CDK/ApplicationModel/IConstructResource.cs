// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Constructs;

namespace Aspire.Hosting.AWS.CDK.ApplicationModel;

public interface IConstructResource : IResource
{
    Construct? GetConstruct();
    IDictionary<string, string> Outputs { get; }
}

public interface IConstructResource<out T> : IConstructResource where T : Construct
{
    T? Construct { get; }
}

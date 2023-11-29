// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Constructs;

namespace Aspire.Hosting.AWS.ApplicationModel;

public interface IConstructResource : IResource;

public interface IConstructResource<out T> : IConstructResource where T : Construct
{
    BuildConstructDelegate<T> Build { get; }
}

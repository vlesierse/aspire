// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Constructs;

namespace Aspire.Hosting.AWS.ApplicationModel;

public class ConstructModifierAnnotation<T>(Action<T> modifier) : IConstructModifierAnnotation
    where T : Construct
{
    public virtual void ChangeConstruct(Construct construct, IConstructResource resource)
    {
        modifier((T)construct);
    }
}

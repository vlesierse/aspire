// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Constructs;

namespace Aspire.Hosting.AWS.CDK.ApplicationModel;

public interface IConstructModifierAnnotation : IResourceAnnotation
{
    void ChangeConstruct(Construct construct, IConstructResource resource);
}

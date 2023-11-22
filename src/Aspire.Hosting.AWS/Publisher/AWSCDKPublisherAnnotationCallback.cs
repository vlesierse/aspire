// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Constructs;

namespace Aspire.Hosting.AWS.Publisher;

public class AWSCDKPublishingAnnotationCallback(Func<Construct, string, Construct>? callback) : IResourceAnnotation
{
    public Func<Construct, string, Construct>? Callback { get; } = callback;

    public static AWSCDKPublishingAnnotationCallback Ignore { get; } = new(null);
}

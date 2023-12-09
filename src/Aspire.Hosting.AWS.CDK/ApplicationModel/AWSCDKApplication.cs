// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CDK.CXAPI;

namespace Aspire.Hosting.AWS.CDK.ApplicationModel;

public class AWSCDKApplication(CloudAssembly assembly)
{
    public CloudAssembly Assembly { get; } = assembly;
}

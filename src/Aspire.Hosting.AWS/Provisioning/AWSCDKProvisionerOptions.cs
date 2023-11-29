// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.AWS.Provisioning;

public class AWSCDKProvisionerOptions
{
    public string StackName { get; set; } = "Aspire";

    public TimeSpan StackPollingDelay { get; set; } = TimeSpan.FromSeconds(3);
}

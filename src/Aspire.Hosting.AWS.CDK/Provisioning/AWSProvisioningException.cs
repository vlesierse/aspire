// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.AWS.CDK.Provisioning;

public class AWSProvisioningException(string message, Exception? innerException)
    : Exception(message, innerException);

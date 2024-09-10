// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Amazon.CDK.CXAPI;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.AWS.CDK;

/// <summary>
/// Annotations that stores the <see cref="CloudFormationStackArtifact"/> to the stack resource.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}, StackName = {StackArtifact.StackName}")]
internal sealed class CloudAssemblyResourceAnnotation(CloudFormationStackArtifact stackArtifact, AssetManifestArtifact? assetsArtifact) : IResourceAnnotation
{

    public AssetManifestArtifact? AssetsArtifact { get; } = assetsArtifact;

    public CloudFormationStackArtifact StackArtifact { get; } = stackArtifact;    
}

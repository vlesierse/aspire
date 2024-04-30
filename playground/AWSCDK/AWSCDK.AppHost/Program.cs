﻿using Amazon;
using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using Attribute = Amazon.CDK.AWS.DynamoDB.Attribute;

var builder = DistributedApplication.CreateBuilder(args).WithAWSCDK();

// Setup a configuration for the AWS .NET SDK.
var awsConfig = builder.AddAWSSDKConfig()
    .WithProfile("default")
    .WithRegion(RegionEndpoint.EUWest1);

/*var stack = builder.AddStack(
        app => new WebAppStack(app, "AspireWebAppStack", new WebAppStackProps()))
    .WithOutput("TableName", s => s.Table.TableName)
    .WithReference(awsConfig);*/

var stack = builder.AddStack("Stack").WithReference(awsConfig);
var table = stack.AddDynamoDBTable("Table", new TableProps
{
    PartitionKey = new Attribute { Name = "id", Type = AttributeType.STRING },
    BillingMode = BillingMode.PAY_PER_REQUEST,
    RemovalPolicy = RemovalPolicy.DESTROY
})
.AddGlobalSecondaryIndex(new GlobalSecondaryIndexProps
{
    IndexName = "OwnerIndex",
    PartitionKey = new Attribute { Name = "owner", Type = AttributeType.STRING },
    SortKey = new Attribute { Name = "ownerSK", Type = AttributeType.STRING },
    ProjectionType = ProjectionType.ALL
});

builder.AddProject<Projects.WebApp>("webapp")
    .WithReference(table);
    //.WithEnvironment("AWS__Resources__TableName", table.GetOutput("TableName", t => t.TableName));
    //.WithEnvironment("AWS__Resources__TableName", table, t => t.TableName);
    //.WithReference(table);

builder.Build().Run();

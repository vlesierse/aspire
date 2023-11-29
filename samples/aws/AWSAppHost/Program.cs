using Amazon.CDK.AWS.DynamoDB;
using Attribute = Amazon.CDK.AWS.DynamoDB.Attribute;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAWSProvisioning();
builder.AddAWSCDKPublisher();

var table = builder.AddConstruct<Table>("table", (scope, name) =>
    new Table(scope, name,
        new TableProps()
        {
            PartitionKey = new Attribute { Name = "id", Type = AttributeType.STRING },
            BillingMode = BillingMode.PAY_PER_REQUEST
        })
);

builder.AddProject<Projects.AWSWebApi>("webapi");
    //.WithReference(table, t => t.TableArn);

builder.Build().Run();

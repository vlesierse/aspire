using Amazon.CDK.AWS.DynamoDB;
using Attribute = Amazon.CDK.AWS.DynamoDB.Attribute;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAWSCDKProvisioning();
builder.AddAWSCDKPublishing();

var table = builder.AddConstruct<Table>("table", (scope, name) =>
    new Table(scope, name,
        new TableProps()
        {
            PartitionKey = new Attribute { Name = "id", Type = AttributeType.STRING },
            BillingMode = BillingMode.PAY_PER_REQUEST
        })
).WithOutput("TableName", table => table.TableName);

builder.AddProject<Projects.WebApp>("webapp").WithReference(table);

builder.Build().Run();

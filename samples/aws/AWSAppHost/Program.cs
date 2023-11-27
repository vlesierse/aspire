var builder = DistributedApplication.CreateBuilder(args);

builder.AddAWSProvisioning();
builder.AddAWSCDKPublisher();

builder.AddProject<Projects.AWSWebApi>("webapi");

builder.Build().Run();

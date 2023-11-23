var builder = DistributedApplication.CreateBuilder(args);

builder.AddAWSCDKPublisher();

builder.AddProject<Projects.AWSWebApi>("webapi");

builder.Build().Run();

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("mymoneymcp-postgres");
var database = postgres.AddDatabase("mymoneymcp-db");

builder.AddProject<Projects.MymoneyMcp_Api>("mymoneymcp-api")
    .WithReference(database)
    .WaitFor(database)
    .WithHttpHealthCheck("/health");

await builder.Build().RunAsync();

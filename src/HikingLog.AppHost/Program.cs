var builder = DistributedApplication.CreateBuilder(args);

var sqlServer = builder
    .AddSqlServer("sql")
    .WithLifetime(ContainerLifetime.Persistent);

var sqlDatabase = sqlServer
    .AddDatabase("hikinglogs");

var api = builder
    .AddProject<Projects.HikingLog_Api>("api")
    .WithReference(sqlDatabase)
    .WaitFor(sqlDatabase)
    .WithHttpEndpoint(port: 5000, targetPort: 8080, name: "http")
    .WithHttpsEndpoint(port: 5001, targetPort: 8081, name: "https");

builder.Build().Run();

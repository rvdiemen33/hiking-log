var builder = DistributedApplication.CreateBuilder(args);

// ============================================
// INFRASTRUCTURE SERVICES
// ============================================

// SQL Server. The image tag is left at the Aspire default so it tracks the supported
// engine version; pin it with .WithImage(...) only when the compose stack has to match exactly.
// Persistent containers survive AppHost restarts for a faster dev loop; they keep running
// after the AppHost stops (remove them with `docker rm -f <name>` when needed).
var sqlServer = builder
    .AddSqlServer("sql")
    .WithLifetime(ContainerLifetime.Persistent);

var sqlDatabase = sqlServer
    .AddDatabase("hikinglogs");

// ============================================
// BACKEND SERVICES
// ============================================

builder
    .AddProject<Projects.HikingLog_Api>("api")
    .WithReference(sqlDatabase)
    .WaitFor(sqlDatabase)
    .WithHttpEndpoint(port: 5000, targetPort: 8090, name: "http")
    .WithHttpsEndpoint(port: 5001, targetPort: 8091, name: "https")

    // Same /health endpoint ServiceDefaults maps, so the dashboard reports the API as
    // Unhealthy when its readiness checks fail. Mapped in Development only, which is the
    // only environment the AppHost runs in.
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

// ============================================
// BUILD AND RUN
// ============================================

await builder.Build().RunAsync();

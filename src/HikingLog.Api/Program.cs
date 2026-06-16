using HikingLog.Application.Extensions;
using HikingLog.Infrastructure.Data;
using HikingLog.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    await DataSeeder.SeedAsync(app.Services);
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

/// <summary>Entry point marker class exposing Program to WebApplicationFactory in integration tests.</summary>
public partial class Program { }

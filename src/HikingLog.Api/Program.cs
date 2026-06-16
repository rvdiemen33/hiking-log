using HikingLog.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

/// <summary>Placeholder weather forecast record — replace with domain models.</summary>
record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    /// <summary>Temperature in Fahrenheit, derived from <see cref="TemperatureC"/>.</summary>
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

/// <summary>Entry point marker class exposing Program to WebApplicationFactory in integration tests.</summary>
public partial class Program { }

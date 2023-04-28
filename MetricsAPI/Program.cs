using MetricsAPI.Interfaces;
using MetricsAPI.Models;
using MetricsAPI.Services;
using System.Dynamic;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddTransient<IMetricLoaderService, MetricLoaderService>();

builder.Services.Configure<MetricsUpdateOptions>(builder.Configuration.GetSection(MetricsUpdateOptions.MetricsUpdate));

builder.Services.AddHostedService<MockBackgroundMetricUpdater>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/metrics/definition/{metricName}", async (string metricName, IMetricLoaderService loader, ILogger<Program> logger) =>
{
    var definition = new MetricDefinition();

    try
    {
        definition = await loader.LoadMetricDefinition(metricName);

        return Results.Ok(definition);
    }
    catch (FileNotFoundException ex)
    {
        logger.LogError(ex.Message);
        return Results.NotFound();
    }
    catch (DirectoryNotFoundException ex)
    {
        logger.LogError(ex.Message);
        return Results.NotFound();
    }
    catch (Exception ex)
    {
        logger.LogError(ex.Message);
        return Results.Problem();
    }
});
app.MapGet("/metrics/total/{metricName}", async (string metricName, IMetricLoaderService loader, ILogger<Program> logger) =>
{
    var total = new MetricData<ExpandoObject>();

    try
    {
        total = await loader.LoadMetricData(metricName, false);

        return Results.Ok(total);
    }
    catch (FileNotFoundException ex)
    {
        logger.LogError(ex.Message);
        return Results.NotFound();
    }
    catch (DirectoryNotFoundException ex)
    {
        logger.LogError(ex.Message);
        return Results.NotFound();
    }
    catch (Exception ex)
    {
        logger.LogError(ex.Message);
        return Results.Problem();
    }
});
app.MapGet("/metrics/inc/{metricName}", async (string metricName, IMetricLoaderService loader, ILogger<Program> logger) =>
{
    var inc = new MetricData<ExpandoObject>();

    try
    {
        inc = await loader.LoadMetricData(metricName, true);

        return Results.Ok(inc);
    }
    catch (FileNotFoundException ex)
    {
        logger.LogError(ex.Message);
        return Results.NotFound();
    }
    catch (DirectoryNotFoundException ex)
    {
        logger.LogError(ex.Message);
        return Results.NotFound();
    }
    catch (Exception ex)
    {
        logger.LogError(ex.Message);
        return Results.Problem();
    }
});
app.Run();
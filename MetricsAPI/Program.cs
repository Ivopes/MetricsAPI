using MetricsAPI.Interfaces;
using MetricsAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddTransient<IMetricLoaderService, MetricLoaderService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Download specific increment metric for specific project
// Obsolete
// TODO: Replace with date specifier
app.MapGet("/metrics/{projectName}/inc/{metricName}", (string projectName, string metricName, IMetricLoaderService loader) =>
{
    return loader.LoadMetric(projectName, metricName, false);
});
// Download specific total metric for specific project
app.MapGet("/metrics/{projectName}/total/{metricName}", (string projectName, string metricName, IMetricLoaderService loader) =>
{
    return loader.LoadMetric(projectName, metricName, true);
});
// Download all increment metric for specific project
app.MapGet("/metrics/{projectName}/inc", (string projectName, IMetricLoaderService loader) =>
{
    return loader.LoadMetricAll(projectName, false);
});
// Download all total metric for specific project
app.MapGet("/metrics/{projectName}/total", (string projectName, IMetricLoaderService loader) =>
{
    return loader.LoadMetricAll(projectName, true);
});
// Download all latest increment metric for specific project
app.MapGet("/metrics/{projectName}/latest", (string projectName, IMetricLoaderService loader) =>
{
    return loader.LoadMetricLatestAll(projectName);
});
// Download specific latest increment metric for specific project
app.MapGet("/metrics/{projectName}/latest/{metricName}", (string projectName, string metricName, IMetricLoaderService loader) =>
{
    return loader.LoadMetricLatest(projectName, metricName);
});
app.Run();
using MetricsAPI.Interfaces;
using MetricsAPI.Models;
using MetricsAPI.Services;

var builder = WebApplication.CreateBuilder(args);

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

app.MapGet("/metrics/definition/{metricName}", (string metricName, IMetricLoaderService loader) =>
{
    return loader.LoadMetricDefinition(metricName);
});
app.MapGet("/metrics/total/{metricName}", (string metricName, IMetricLoaderService loader) =>
{
    return loader.LoadMetricData(metricName, false);
});
app.MapGet("/metrics/inc/{metricName}", (string metricName, IMetricLoaderService loader) =>
{
    return loader.LoadMetricData(metricName, true);
});
app.Run();
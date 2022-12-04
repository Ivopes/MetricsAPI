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

app.MapGet("/metrics/inc/{metricName}", (string metricName, IMetricLoaderService loader) =>
{
    return loader.LoadMetric(metricName, false);
});
app.MapGet("/metrics/total/{metricName}", (string metricName, IMetricLoaderService loader) =>
{
    return loader.LoadMetric(metricName, true);
})

.WithName("GetSpecifiedMetric");

app.Run();
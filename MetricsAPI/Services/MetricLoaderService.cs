using MetricsAPI.Interfaces;
using MetricsAPI.Models;

namespace MetricsAPI.Services
{
    public class MetricLoaderService : IMetricLoaderService
    {
        private readonly string _metricsFolder = @"C:\Users\hapes\Downloads\Metriky";

        public async Task<IResult> LoadMetric(string metricName)
        {
            List<MetricPortion> metrics = new();

            bool skipFirst = true;
            foreach (var line in File.ReadLines(Path.Combine(_metricsFolder, metricName + ".csv")))
            {
                if (skipFirst)
                {
                    skipFirst = false;
                    continue;
                }
                var metric = new MetricPortion();

                string[] fields = line.Split(';');

                try
                {
                    metric.Date = DateTime.Parse(fields[0]);
                    metric.AdditionWithSign = int.Parse(fields[1]);
                    metric.AdditionWithoutSign = int.Parse(fields[2]);
                }
                catch (Exception ex)
                {
                    return Results.Problem(statusCode: 500, detail: "Loaded file has wrong format");
                }
                metrics.Add(metric);
            }

            var r = new { Name = metricName, Rows = metrics };

            return Results.Ok(r);
        }
    }
}

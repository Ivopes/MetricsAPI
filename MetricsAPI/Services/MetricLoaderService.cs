using MetricsAPI.Interfaces;
using MetricsAPI.Models;

namespace MetricsAPI.Services
{
    public class MetricLoaderService : IMetricLoaderService
    {
        private readonly string _metricsIncFolder = @".\Metriky\Increment";
        private readonly string _metricsTotalFolder = @".\Metriky\Total";

        public async Task<IResult> LoadMetric(string metricName, bool isTotal)
        {
            List<MetricPortion> metrics = new();

            string filePath = string.Empty;
            if (isTotal)
                filePath = Path.Combine(_metricsTotalFolder, metricName + ".csv");
            else
                filePath = Path.Combine(_metricsIncFolder, metricName + ".csv");

            bool skipFirst = false;

            foreach (var line in await File.ReadAllLinesAsync(filePath))
            {
                if (!skipFirst)
                { 
                    skipFirst= true;
                    continue; 
                }

                var metric = new MetricPortion();

                string[] fields = line.Split(';');

                try
                {
                    metric.Date = DateTime.Parse(fields[0]);
                    metric.AdditionWithSign = int.Parse(fields[1]);
                    metric.AdditionWithoutSign = int.Parse(fields[2]);
                    metric.Release = fields[3];
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

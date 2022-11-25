namespace MetricsAPI
{
    public class MetricLoaderBase
    {

        public IResult LoadMetric(string metricName)
        {
            return Results.Ok(metricName);
        }
    }
}
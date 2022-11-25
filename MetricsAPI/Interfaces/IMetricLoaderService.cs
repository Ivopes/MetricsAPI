namespace MetricsAPI.Interfaces
{
    public interface IMetricLoaderService
    {
        Task<IResult> LoadMetric(string metricName);
    }
}
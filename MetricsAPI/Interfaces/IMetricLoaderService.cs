using MetricsAPI.Models;
using System.Dynamic;

namespace MetricsAPI.Interfaces
{
    public interface IMetricLoaderService
    {
        Task<MetricData<ExpandoObject>?> LoadMetricData(string metricName, bool loadIncrement);
        Task<IResult> LoadMetricDefinition(string metricName);
    }
}
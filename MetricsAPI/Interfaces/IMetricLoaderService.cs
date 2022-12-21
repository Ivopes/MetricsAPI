namespace MetricsAPI.Interfaces
{
    public interface IMetricLoaderService
    {
        /// <summary>
        /// Download specific total/increment metric for specific project
        /// </summary>
        /// <param name="isTotal">Do you want total or increment</param>
        Task<IResult> LoadMetric(string projectName, string metricName, bool isTotal);
        /// <summary>
        /// Download all total/increment metric for specific project
        /// </summary>
        /// <param name="isTotal">Do you want total or increment</param>
        Task<IResult> LoadMetricAll(string projectName, bool isTotal);
        /// <summary>
        /// Download specific latest increment metric for specific project
        /// </summary>
        Task<IResult> LoadMetricLatest(string projectName, string metricName);
        /// <summary>
        /// // Download all latest increment metric for specific project
        /// </summary>
        Task<IResult> LoadMetricLatestAll(string projectName);
    }
}
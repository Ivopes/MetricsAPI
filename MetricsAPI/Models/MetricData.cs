namespace MetricsAPI.Models
{
    public class MetricData<T>
    {
        public ICollection<T> Rows { get; set; } = Array.Empty<T>().ToList();
    }
}

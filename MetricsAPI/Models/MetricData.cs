namespace MetricsAPI.Models
{
    public class MetricData<T>
    {
        public string Name { get; set; }
        public ICollection<T> Rows { get; set; } = Array.Empty<T>().ToList();
    }
}

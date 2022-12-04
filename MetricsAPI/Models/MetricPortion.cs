namespace MetricsAPI.Models
{
    public class MetricPortion
    {
        public DateTime Date { get; set; }
        public int AdditionWithSign { get; set; }
        public int AdditionWithoutSign { get; set; }
        public string Release { get; set; }
    }
}

namespace MetricsAPI.Models
{
    public class MetricsUpdateOptions
    {
     
            public const string MetricsUpdate = "MetricsUpdate";
            public UpdateFrequency UpdateFrequency { get; set; }
            public DayOfWeek DayOfWeek { get; set; }
    }
    public enum UpdateFrequency
    {
        Hour = 0,
        Day = 1,
        Week = 2
    }
}

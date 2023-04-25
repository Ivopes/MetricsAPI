namespace MetricsAPI.Models
{
    public class MetricsUpdateOptions
    {

        public const string MetricsUpdate = "MetricsUpdate";
        public UpdateFrequency UpdateFrequency { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        private int _hour;
        public int Hour
        {
            get => _hour;
            set => _hour = Math.Clamp(value, 0, 23);
        }
        public MockUpdate MockUpdate { get; set; } = new();
    }
    public enum UpdateFrequency
    {
        Hour = 0,
        Day = 1,
        Week = 2
    }
    public class MockUpdate
    {
        public bool Enabled { get; set; }
        private int _minute;
        public int Minute
        {
            get => _minute;
            set => _minute = Math.Clamp(value, 0, 59);
        }
    }
}

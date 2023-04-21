using MetricsAPI.Interfaces;
using MetricsAPI.Models;
using Microsoft.Extensions.Options;

namespace MetricsAPI.Services
{
    public class MockBackgroundMetricUpdater : BackgroundService
    {
        private readonly MetricsUpdateMockOptions _updateOptions = new();
        private readonly PeriodicTimer _timer = new(TimeSpan.FromHours(1));
        private readonly IServiceProvider _serviceProvider;

        public MockBackgroundMetricUpdater(IOptions<MetricsUpdateMockOptions> opt, IServiceProvider serviceProvider)
        {
            if (opt.Value is not null)
            {
                _updateOptions = new()
                {
                    DayOfWeek = opt.Value.DayOfWeek,
                    Hour = opt.Value.Hour,
                    UpdateFrequency = opt.Value.UpdateFrequency,
                    Enabled = opt.Value.Enabled
                };
            }

            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_updateOptions.Enabled) { return; }

            //Pockat pro celou hodinu
            var diffToZeroMinutes = (60 - DateTime.UtcNow.Minute) % 60;
            await Task.Delay(TimeSpan.FromMinutes(diffToZeroMinutes), stoppingToken);

            if (ShouldUpdate())
                await UpdateMetrics();

            while (await _timer.WaitForNextTickAsync(stoppingToken)
               && !stoppingToken.IsCancellationRequested)
            {
                if (ShouldUpdate())
                {
                    await UpdateMetrics();
                }
            }
        }
        private async Task UpdateMetrics()
        {
            var metricFolder = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Metrics");

            var folders = Directory.GetDirectories(metricFolder);

            foreach (var folder in folders)
            {
                var def = Directory.GetFiles(Path.Combine(folder, "Definition"))[0];

                var ext = Path.GetExtension(def);

                if (ext == ".csv")
                {
                    var oldInc = await AddRandomIncrementCSV(folder);

                    await AddTotalFromIncrement(folder, oldInc);
                }
            }
        }
        /// <summary>
        /// Adds random increment and returns the old one
        /// </summary>
        private async Task<string> AddRandomIncrementCSV(string path)
        {

            string toAdd = string.Empty;
            string fileName = new DirectoryInfo(path).Name;
            await using var scope = _serviceProvider.CreateAsyncScope();

            var metricLoader = scope.ServiceProvider.GetRequiredService<IMetricLoaderService>();

            var metric = await metricLoader.LoadMetricData(fileName, true);

            DateTime lastDate = DateTime.Parse((metric!.Rows.Last() as IDictionary<string, object>)["Date"].ToString()!);

            var rand = new Random();
            int max = 3;
            if (_updateOptions.UpdateFrequency == UpdateFrequency.Week) max = 7;

            for (int i = 0; i < max; i++)
            {
                toAdd += _updateOptions.UpdateFrequency switch
                {
                    UpdateFrequency.Hour => DateTime.UtcNow.ToString("dd.MM.yyyy") + ";",
                    UpdateFrequency.Day => DateTime.UtcNow.AddDays(-1).ToString("dd.MM.yyyy") + ";",
                    UpdateFrequency.Week => DateTime.UtcNow.AddDays(-(max - i)).ToString("dd.MM.yyyy") + ";",
                };
                toAdd += rand.Next(3, 11) + ";";
                toAdd += rand.Next(10, 15) + ";";
                toAdd += (i % 3 + 1).ToString() + Environment.NewLine;
            }


            var filePath = Directory.GetFiles(Path.Combine(path, "Increment"))[0];
            var metricString = await File.ReadAllTextAsync(filePath);

            fileName += "_" + GetNewUpdateSuffix() + ".csv";
            fileName = fileName[(fileName.IndexOf("_") + 1)..];

            await File.WriteAllTextAsync(Path.Combine(path, "Increment", fileName), toAdd);

            return metricString;
        }
        private async Task AddTotalFromIncrement(string path, string increment)
        {
            var filePath = Directory.GetFiles(Path.Combine(path, "Total"))[0];

            await File.AppendAllTextAsync(filePath, increment);
        }
        private string GetNewUpdateSuffix()
        {
            DateTime lastMetricDate = DateTime.UtcNow;

            switch (_updateOptions.UpdateFrequency)
            {
                case UpdateFrequency.Hour:
                    //lastMetricDate = lastMetricDate.AddHours(-1);
                    return lastMetricDate.ToString("HH_dd_MM_yyyy");
                case UpdateFrequency.Day:
                    //lastMetricDate = lastMetricDate.AddDays(-1);
                    return lastMetricDate.ToString("00_dd_MM_yyyy");
                case UpdateFrequency.Week:
                    //while (lastMetricDate.DayOfWeek != _updateOptions.DayOfWeek) lastMetricDate = lastMetricDate.AddDays(-1);
                    return lastMetricDate.ToString("00_dd_MM_yyyy");
                default:
                    return string.Empty;
            }
        }
        private bool ShouldUpdate()
        {
            var now = DateTime.UtcNow;
            switch (_updateOptions.UpdateFrequency)
            {
                case UpdateFrequency.Hour:
                    return true;
                case UpdateFrequency.Day:
                    return now.Hour == _updateOptions.Hour;
                case UpdateFrequency.Week:
                    return now.Hour == _updateOptions.Hour && now.DayOfWeek == _updateOptions.DayOfWeek;
                default:
                    return false;
            }
        }
    }
}

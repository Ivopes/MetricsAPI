using MetricsAPI.Interfaces;
using MetricsAPI.Models;
using Microsoft.Extensions.Options;
using System.IO;

namespace MetricsAPI.Services
{
    public class MockBackgroundMetricUpdater : BackgroundService
    {
        private readonly MetricsUpdateOptions _updateOptions;
        private readonly PeriodicTimer _timer = new(TimeSpan.FromHours(1));
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MockBackgroundMetricUpdater> _logger;
        public MockBackgroundMetricUpdater(IOptions<MetricsUpdateOptions> opt, IServiceProvider serviceProvider, ILogger<MockBackgroundMetricUpdater> logger)
        {
            _updateOptions = opt.Value;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_updateOptions.MockUpdate.Enabled) { return; }
            _logger.LogInformation($"Starting executing {nameof(MockBackgroundMetricUpdater)}...");

            //Pockat pro celou hodinu
            var diffToZeroMinutes = (_updateOptions.MockUpdate.Minute - DateTime.UtcNow.Minute + 60) % 60;
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
            _logger.LogInformation($"Starting {nameof(MockBackgroundMetricUpdater)} update...");


            var metricFolder = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Metrics");

            var folders = Directory.GetDirectories(metricFolder);

            foreach (var folder in folders)
            {
                if (folder.EndsWith("Projekt1_FinishedReq") || folder.EndsWith("Projekt1_ApprovedReq"))
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

            string fileName = new DirectoryInfo(path).Name;
            string filePath = Directory.GetFiles(Path.Combine(path, "Increment"))[0];
            var metricString = await File.ReadAllTextAsync(filePath);

            fileName += "_" + GetNewUpdateSuffix() + ".csv";
            fileName = fileName[(fileName.IndexOf("_") + 1)..];

            File.Delete(filePath);

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
                    lastMetricDate = lastMetricDate.AddHours(-1);
                    return lastMetricDate.ToString("HH_dd_MM_yyyy");
                case UpdateFrequency.Day:
                    lastMetricDate = lastMetricDate.AddDays(-1);
                    return lastMetricDate.ToString("00_dd_MM_yyyy");
                case UpdateFrequency.Week:
                    while (lastMetricDate.DayOfWeek != _updateOptions.DayOfWeek) lastMetricDate = lastMetricDate.AddDays(-1);
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

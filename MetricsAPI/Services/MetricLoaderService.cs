using MetricsAPI.Interfaces;
using MetricsAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics.Metrics;
using System.Dynamic;
using System.IO;

namespace MetricsAPI.Services
{
    public class MetricLoaderService : IMetricLoaderService
    {
        private const string MetricsDefinitionFolder = @".\Metriky\Nove\{0}\Definition\metricDefinition.csv";
        private const string MetricsIncFolder = @".\Metriky\Nove\{0}\Increment\{1}";
        private const string MetricsTotalFolder = @".\Metriky\Nove\{0}\Total\{1}";

        private const string FileExt = ".csv";
        private readonly MetricsUpdateOptions _updateOptions;
        public MetricLoaderService(IOptions<MetricsUpdateOptions> opt)
        {
            _updateOptions = opt.Value;
        }
        public async Task<IResult> LoadMetricDefinition(string metricName)
        {
            var definition = new MetricDefinition();

            try
            {
                definition = await ReadDefinitionFromFile(metricName);

                return Results.Ok(definition);
            }
            catch (FileNotFoundException)
            {
                return Results.NotFound();
            }
            catch (DirectoryNotFoundException)
            {
                return Results.NotFound();
            }
        }
        public async Task<MetricData<ExpandoObject>?> LoadMetricData(string metricName, bool loadIncrement)
        {
            CreateFilePath(metricName, loadIncrement, FileExt, out var filePath, out var fileDefinition);

            var definition = await ReadDefinitionFromFile(metricName);

            MetricData<ExpandoObject> metricData = await ReadMetricFromFile(filePath, definition);

            return metricData;
        }
        private async Task<MetricData<ExpandoObject>> ReadMetricFromFile(string filePath, MetricDefinition definition)
        {
            MetricData<ExpandoObject> metrics = new();
            metrics.Rows = new List<ExpandoObject>();
            
            using Stream s = File.OpenRead(filePath);

            using StreamReader sr = new StreamReader(s);

            while (!sr.EndOfStream)
            {
                string[] line = (await sr.ReadLineAsync())!.Split(';');

                var obj = new ExpandoObject() as IDictionary<string, Object>; ;
                for (int i = 0; i < definition.ColumnNames.Count; i++)
                {
                    if (definition.ColumnTypes[i] == "Int64")
                    {
                        obj.Add(definition.ColumnNames[i], int.Parse(line[i]));
                    }
                    else if (definition.ColumnTypes[i] == "Datetime")
                    {
                        obj.Add(definition.ColumnNames[i], DateTime.Parse(line[i]));
                    }
                    else
                    {
                        obj.Add(definition.ColumnNames[i], line[i]);
                    }
                }

                metrics.Rows.Add((obj as ExpandoObject)!);
            }

            return metrics;
        }
        private async Task<MetricDefinition> ReadDefinitionFromFile(string metricName)
        {
            MetricDefinition definition = new();

            var exePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, MetricsDefinitionFolder);
            string filePath = string.Format(exePath, metricName);

            using Stream s = File.OpenRead(filePath);

            using StreamReader sr = new StreamReader(s);

            definition.Name = await sr.ReadLineAsync()!;

            _ = await sr.ReadLineAsync();

            definition.ColumnNames = new List<string>();
            definition.ColumnTypes = new List<string>();
            string? line;
            while ((line = await sr.ReadLineAsync()) != ";" && !string.IsNullOrEmpty(line))
            {
                string[] columns = line.Split(';');

                definition.ColumnNames.Add(columns[0]);
                definition.ColumnTypes.Add(columns[1]);
            }

            _ = await sr.ReadLineAsync();

            definition.Measures = new List<string>();
            definition.MeasureDefinitions = new List<string>();
            while ((line = await sr.ReadLineAsync()) != ";" && !string.IsNullOrEmpty(line))
            {
                string[] columns = line.Split(';');

                definition.Measures.Add(columns[0]);
                definition.MeasureDefinitions.Add(columns[1]);
            }

            return definition;
        }
        private void CreateFilePath(string metricName, bool loadIncrement, string fileExtention, out string filePath, out string fileDefinition)
        {
            var nameStartIndex = metricName.IndexOf("_");
            //if (nameStartIndex == -1) return string.Empty;

            string fileName = metricName.Substring(nameStartIndex + 1);

            if (loadIncrement)
            {
                string date = GetUpdateSuffix();

                var exePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, MetricsIncFolder);
                filePath = string.Format(exePath, metricName, fileName + "_" + date + fileExtention);
            }
            else
            {
                filePath = string.Format(MetricsTotalFolder, metricName, fileName + fileExtention);
            }

            fileDefinition = string.Format(MetricsDefinitionFolder, metricName);
        }
        private string GetUpdateSuffix()
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
    }
}

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
        private const string MetricsIncFolder = @".\Metriky\{0}\Increment\{1}";
        private const string MetricsTotalFolder = @".\Metriky\{0}\Total\{1}";
        private const string MetricsDefinitionFolder = @".\Metriky\{0}\Definition\{1}";

        private const string MetricsDefinitionFolderNew = @".\Metriky\Nove\{0}\Definition\metricDefinition.csv";
        private const string MetricsIncFolderNew = @".\Metriky\Nove\{0}\Increment\{1}";
        private const string MetricsTotalFolderNew = @".\Metriky\Nove\{0}\Total\{1}";

        private const string FileExt = ".csv";
        private readonly MetricsUpdateOptions _updateOptions;
        public MetricLoaderService(IOptions<MetricsUpdateOptions> opt)
        {
            _updateOptions = opt.Value;
        }
        public async Task<IResult> LoadMetric(string projectName, string metricName, bool isTotal)
        {
            try
            {
                MetricPortion metrics = await ReadMetricFromFile(projectName, metricName, isTotal);

                return Results.Ok(metrics);
            } 
            catch (FileNotFoundException) 
            {
                return Results.NotFound();
            }
            catch (DirectoryNotFoundException)
            {
                return Results.NotFound();
            }
            catch (Exception)
            {
                return Results.Problem(statusCode: 500);
            }
        }

        public async Task<IResult> LoadMetricAll(string projectName, bool isTotal)
        {
            List<MetricPortion> metrics = new List<MetricPortion>();
            try
            {
                string path;
                if (isTotal)
                    path = string.Format(MetricsTotalFolder, projectName, string.Empty);
                else
                    path = string.Format(MetricsIncFolder, projectName, string.Empty);

                foreach (string file in Directory.EnumerateFiles(path, $"*{FileExt}"))
                {
                    var fileName = file.Substring(0, file.Length - FileExt.Length).Substring(file.LastIndexOf('\\')+1);
                    var metric = await ReadMetricFromFile(projectName, fileName, isTotal);
                    metrics.Add(metric);
                }
            }
              catch (FileNotFoundException)
            {
                return Results.NotFound();
            }
            catch (DirectoryNotFoundException)
            {
                return Results.NotFound();
            }

            return Results.Ok(metrics);
        }

        public async Task<IResult> LoadMetricDefinition(string projectName, string metricName)
        {
            var definition = new MetricDefinition();

            string filePath = string.Format(MetricsDefinitionFolder, projectName, metricName);
            try
            {
                if (Directory.GetFiles(filePath, $"*{FileExt}").Length != 1) return Results.Problem($"Weird structure inside folder: {filePath}");

                definition = await ReadDefinitionFromFile(projectName, metricName, FileExt);

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
        public async Task<IResult> LoadMetricDefinitionNew(string metricName)
        {
            var definition = new MetricDefinition();

            try
            {
                definition = await ReadDefinitionFromFileNew(metricName);

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
        public async Task<IResult> LoadMetricLatest(string projectName, string metricName)
        {
            var metric = new MetricPortion();

            string path = string.Format(MetricsIncFolder, projectName, string.Empty);
            IOrderedEnumerable<string> files;
            try
            {
                files = Directory.GetFiles(path, $"*{FileExt}").Where(f => string.Equals(TrimNumber(Path.GetFileNameWithoutExtension(f)), metricName, StringComparison.InvariantCultureIgnoreCase)).Select(f => Path.GetFileName(f)).OrderByDescending(f => f);
            
                if (files.Count() == 0) return Results.NotFound();

                var latestFile = files.First();

                metric = await ReadMetricFromFile(projectName, latestFile, false, string.Empty); // already has extention in name
            }
            catch (FileNotFoundException)
            {
                return Results.NotFound();
            }
            catch (DirectoryNotFoundException)
            {
                return Results.NotFound();
            }

            return Results.Ok(metric);
        }

        public async Task<IResult> LoadMetricLatestAll(string projectName)
        {
            var metrics = new List<MetricPortion>();

            string path = string.Format(MetricsIncFolder, projectName, string.Empty);
            IOrderedEnumerable<string> files;
            try
            {
                files = Directory.GetFiles(path, $"*{FileExt}").Select(f => Path.GetFileName(f)).OrderByDescending(f => f);
            }
            catch (FileNotFoundException)
            {
                return Results.NotFound();
            }
            catch (DirectoryNotFoundException)
            {
                return Results.NotFound();
            }

            var distinctFilesGroup = files.GroupBy(f => TrimNumber(f));

            foreach (var fileGroup in distinctFilesGroup)
            {
                var latestFile = fileGroup.OrderByDescending(f => f).First();

                metrics.Add(await ReadMetricFromFile(projectName, latestFile, false, string.Empty)); // already has extention in name
            }
       
            return Results.Ok(metrics);
        }
        public async Task<MetricData<ExpandoObject>?> LoadMetricDataNew(string metricName, bool loadIncrement)
        {
            CreateFilePath(metricName, loadIncrement, FileExt, out var filePath, out var fileDefinition);

            var definition = await ReadDefinitionFromFileNew(metricName);

            MetricData<ExpandoObject> metricData = await ReadMetricFromFileNew(filePath, definition);

            return metricData;
        }
        private Task<MetricPortion> ReadMetricFromFile(string projectName, string metricName, bool isTotal)
        {
            return ReadMetricFromFile(projectName, metricName, isTotal, FileExt);
        }
        private async Task<MetricPortion> ReadMetricFromFile(string projectName, string metricName, bool isTotal, string fileExtention)
        {
            MetricPortion metrics = new();

            metrics.Name = metricName;

            string filePath;
            if (isTotal)
                filePath = string.Format(MetricsTotalFolder, projectName, metricName + fileExtention);
            else
                filePath = string.Format(MetricsIncFolder, projectName, metricName + fileExtention);

            using Stream s = File.OpenRead(filePath);

            using StreamReader sr = new StreamReader(s);

            string[] line = (await sr.ReadLineAsync())!.Split(';');

            metrics.AdditionWithSignName = line[1];
            metrics.AdditionWithoutSignName = line[2];

            line = (await sr.ReadLineAsync())!.Split(';');

            metrics.AdditionWithSignType = Type.GetType(line[1])!.FullName!;

            metrics.AdditionWithoutSignType = Type.GetType(line[2])!.FullName!;

            while (!sr.EndOfStream)
            {
                var row = new MetricPortionRow();

                line = (await sr.ReadLineAsync())!.Split(';');

                row.Date = DateTime.Parse(line[0]);
                row.AdditionWithSign = float.Parse(line[1]);
                row.AdditionWithoutSign = float.Parse(line[2]);
                row.Release = line[3];

                metrics.Rows.Add(row);
            }

            return metrics;
        }
        private async Task<MetricData<ExpandoObject>> ReadMetricFromFileNew(string filePath, MetricDefinition definition)
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
        private async Task<MetricDefinition> ReadDefinitionFromFile(string projectName, string metricName, string fileExtention)
        {
            MetricDefinition definition = new();

            definition.Name = metricName;

            string filePath = string.Format(MetricsDefinitionFolder, projectName, metricName + fileExtention);

            using Stream s = File.OpenRead(filePath);

            using StreamReader sr = new StreamReader(s);

            _ = await sr.ReadLineAsync();

            definition.ColumnNames = new List<string>();
            definition.ColumnTypes = new List<string>();
            string? line;
            while ((line = await sr.ReadLineAsync() ) != ";" && !string.IsNullOrEmpty(line))
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
        private async Task<MetricDefinition> ReadDefinitionFromFileNew(string metricName)
        {
            MetricDefinition definition = new();

            var exePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, MetricsDefinitionFolderNew);
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
        private string TrimNumber(string file)
        {
            return file.Substring(0, file.LastIndexOf("_"));
        }

        private void CreateFilePath(string metricName, bool loadIncrement, string fileExtention, out string filePath, out string fileDefinition)
        {
            var nameStartIndex = metricName.IndexOf("_");
            //if (nameStartIndex == -1) return string.Empty;

            string fileName = metricName.Substring(nameStartIndex + 1);

            if (loadIncrement)
            {
                string date = GetUpdateSuffix();

                var exePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, MetricsIncFolderNew);
                filePath = string.Format(exePath, metricName, fileName + "_" + date + fileExtention);
            }
            else
            {
                filePath = string.Format(MetricsTotalFolderNew, metricName, fileName + fileExtention);
            }

            fileDefinition = string.Format(MetricsDefinitionFolderNew, metricName);
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

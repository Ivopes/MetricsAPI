using MetricsAPI.Interfaces;
using MetricsAPI.Models;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace MetricsAPI.Services
{
    public class MetricLoaderService : IMetricLoaderService
    {
        private const string MetricsIncFolder = @".\Metriky\{0}\Increment\{1}";
        private const string MetricsTotalFolder = @".\Metriky\{0}\Total\{1}";
        private const string FileExt = ".csv";

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
        private string TrimNumber(string file)
        {
            return file.Substring(0, file.LastIndexOf("_"));
        }
    }
}

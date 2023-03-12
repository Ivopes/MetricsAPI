﻿using MetricsAPI.Interfaces;
using MetricsAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics.Metrics;
using System.Dynamic;
using System.IO;
using System.Text.Json;

namespace MetricsAPI.Services
{
    public class MetricLoaderService : IMetricLoaderService
    {
        private const string MetricsDefinitionFolder = @".\Metriky\Nove\{0}\Definition\metricDefinition";
        private const string MetricsIncFolder = @".\Metriky\Nove\{0}\Increment\{1}";
        private const string MetricsTotalFolder = @".\Metriky\Nove\{0}\Total\{1}";

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
            var definition = await ReadDefinitionFromFile(metricName);

            CreateFilePath(metricName, loadIncrement, GetExt(FileExt.CSV), out var filePath, out _);

            if (File.Exists(filePath))
            {
                return await ReadMetricFromFileCsv(filePath, definition);
            }
            else
            {
                CreateFilePath(metricName, loadIncrement, GetExt(FileExt.JSON), out filePath, out _);
                return await ReadMetricFromFileJson(filePath, definition);
            }
        }
        private async Task<MetricData<ExpandoObject>> ReadMetricFromFileCsv(string filePath, MetricDefinition definition)
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
        private async Task<MetricData<ExpandoObject>> ReadMetricFromFileJson(string filePath, MetricDefinition definition)
        {
            MetricData<ExpandoObject> metrics = new();
            metrics.Rows = new List<ExpandoObject>();

            using Stream s = File.OpenRead(filePath);

            metrics.Rows = (await JsonSerializer.DeserializeAsync<ExpandoObject[]>(s))!;

            return metrics;
        }
        private async Task<MetricDefinition> ReadDefinitionFromFile(string metricName)
        {

            var exePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, MetricsDefinitionFolder);
            string filePath = string.Format(exePath, metricName);


            if (File.Exists(filePath + GetExt(FileExt.CSV))) 
            {
                filePath += GetExt(FileExt.CSV);
                return await ReadDefinitionFromCsv(filePath);
            }
            else
            {
                filePath += GetExt(FileExt.JSON);
                return await ReadDefinitionFromJson(filePath);
            }
        }
        private async Task<MetricDefinition> ReadDefinitionFromCsv(string filePath)
        {
            MetricDefinition definition = new();

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
        private async Task<MetricDefinition> ReadDefinitionFromJson(string filePath)
        {
            using Stream s = File.OpenRead(filePath);

            MetricDefinition definition = (await JsonSerializer.DeserializeAsync<MetricDefinition>(s))!;

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
        private string GetExt(FileExt ext)
        {
            return ext switch
            {
                FileExt.CSV => ".csv",
                FileExt.JSON => ".json",
                _ => string.Empty
            };
        }
    }
    public enum FileExt
    {
        CSV = 0,
        JSON = 1
    }
}

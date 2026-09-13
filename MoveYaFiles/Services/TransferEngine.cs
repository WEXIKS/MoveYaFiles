using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using MoveYaFiles.Models;

namespace MoveYaFiles.Services;

public class TransferEngine
{
    private const string ConfigFilePath = "config.json";

    public AppConfig LoadConfig()
    {
        if (!File.Exists(ConfigFilePath))
        {
            var defaultConfig = new AppConfig();
            SaveConfig(defaultConfig);
            return defaultConfig;
        }

        try
        {
            var json = File.ReadAllText(ConfigFilePath);
            return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
        }
        catch
        {
            return new AppConfig();
        }
    }

    public void SaveConfig(AppConfig config)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        var json = JsonSerializer.Serialize(config, options);
        File.WriteAllText(ConfigFilePath, json);
    }

    public void ExecuteTransfer(AppConfig config)
    {
        foreach (var rule in config.Rules)
        {
            if (string.IsNullOrWhiteSpace(rule.SourcePath) || !Directory.Exists(rule.SourcePath))
                continue;

            if (string.IsNullOrWhiteSpace(rule.DestinationPath))
                continue;

            if (!Directory.Exists(rule.DestinationPath))
            {
                Directory.CreateDirectory(rule.DestinationPath);
            }

            var files = Directory.GetFiles(rule.SourcePath);
            foreach (var filePath in files)
            {
                var fileInfo = new FileInfo(filePath);

                // Warunek rozszerzenia
                if (rule.AllowedExtensions != null && rule.AllowedExtensions.Length > 0)
                {
                    string fileExt = fileInfo.Extension.TrimStart('.').ToLower();
                    bool isAllowed = rule.AllowedExtensions.Any(ext =>
                        ext.TrimStart('.').Equals(fileExt, StringComparison.OrdinalIgnoreCase));

                    if (!isAllowed) continue;
                }

                // Warunek rozmiaru (sprawdzamy MaxFileSize tylko gdy jest > 0)
                if (fileInfo.Length < rule.MinFileSizeBytes) continue;
                if (rule.MaxFileSizeBytes > 0 && fileInfo.Length > rule.MaxFileSizeBytes) continue;

                // Rozwiązywanie konfliktów nazw
                var destinationFilePath = Path.Combine(rule.DestinationPath, fileInfo.Name);

                if (File.Exists(destinationFilePath))
                {
                    string strategy = rule.ConflictStrategy ?? "Skip";

                    if (strategy.Contains("Skip"))
                    {
                        continue;
                    }
                    else if (strategy.Contains("Custom"))
                    {
                        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileInfo.Name);
                        var suffix = string.IsNullOrWhiteSpace(rule.CustomSuffix) ? "_copy" : rule.CustomSuffix;
                        destinationFilePath = Path.Combine(rule.DestinationPath, $"{fileNameWithoutExt}{suffix}{fileInfo.Extension}");
                    }
                    else if (strategy.Contains("Timestamp"))
                    {
                        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileInfo.Name);
                        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        destinationFilePath = Path.Combine(rule.DestinationPath, $"{fileNameWithoutExt}_{timestamp}{fileInfo.Extension}");
                    }
                    
                }

                // Bezpieczny transfer pliku
                try
                {
                    File.Copy(filePath, destinationFilePath, overwrite: true);
                   
                }
                catch
                {
                    
                }
            }
        }

        config.LastRun = DateTime.Now;
        SaveConfig(config);
    }
}
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
            if(string.IsNullOrWhiteSpace(rule.SourcePath) || !Directory.Exists(rule.SourcePath))
                continue;
            if (!Directory.Exists(rule.DestinationPath))
            {
                Directory.CreateDirectory(rule.DestinationPath);
            }

            var files = Directory.GetFiles(rule.SourcePath);
            foreach (var filePath in files)
            {
                var fileInfo = new FileInfo(filePath);
                //war.rozszerzenia
                if (rule.AllowedExtensions != null && rule.AllowedExtensions.Length > 0)
                {
                    bool isAllowed = rule.AllowedExtensions.Any(ext =>
                        ext.Equals(fileInfo.Extension, StringComparison.OrdinalIgnoreCase));
                    
                    if (!isAllowed) continue;
                }
                //war.rozmiaru
                if (fileInfo.Length < rule.MinFileSizeBytes || fileInfo.Length > rule.MaxFileSizeBytes)
                {
                    continue;
                }
                //rozwiazywanie problemow z konfliktem nazw
                var destinationFilePath = Path.Combine(rule.DestinationPath, fileInfo.Name);
                if (File.Exists(destinationFilePath))
                {
                    switch (rule.ConflictStrategy)
                    {
                        case "Skip":
                            continue;
                        case"AddTimestamp":
                            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileInfo.Name);
                            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                            var newFileName = $"{fileNameWithoutExt}_{timestamp}{fileInfo.Extension}";
                            destinationFilePath = Path.Combine(rule.DestinationPath, newFileName);
                            break;
                        case"Overwrite":
                            //przegrywa domyslnie  z flaga overwrite=true
                            break;
                    }
                }
                File.Copy(filePath, destinationFilePath, overwrite: true);
            }
        }

        config.LastRun = DateTime.Now;
        SaveConfig(config);
    }
}


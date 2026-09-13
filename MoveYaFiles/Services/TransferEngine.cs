using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using MoveYaFiles.Models;

namespace MoveYaFiles.Services;

public class TransferEngine
{
    private const string ConfigFileName = "config.json";

    public AppConfig LoadConfig()
    {
        if (!File.Exists(ConfigFileName))
            return new AppConfig();

        try
        {
            string json = File.ReadAllText(ConfigFileName);
            return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
        }
        catch
        {
            return new AppConfig();
        }
    }

    public void SaveConfig(AppConfig config)
    {
        try
        {
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFileName, json);
        }
        catch
        {
            // Ignorowanie błędów zapisu
        }
    }

    public (int totalMoved, List<LogEntry> newLogs) ExecuteTransfer(AppConfig config)
    {
        int totalFilesMoved = 0;
        var newLogs = new List<LogEntry>();

        foreach (var rule in config.Rules)
        {
            if (string.IsNullOrWhiteSpace(rule.SourcePath) || string.IsNullOrWhiteSpace(rule.DestinationPath))
            {
                continue;
            }

            if (!Directory.Exists(rule.SourcePath))
            {
                newLogs.Add(new LogEntry
                {
                    RuleName = rule.DisplayName,
                    Status = "Error",
                    Message = $"Ścieżka źródłowa nie istnieje: {rule.SourcePath}"
                });
                continue;
            }

            try
            {
                if (!Directory.Exists(rule.DestinationPath))
                    Directory.CreateDirectory(rule.DestinationPath);

                var files = Directory.GetFiles(rule.SourcePath);
                int movedForRule = 0;

                foreach (var filePath in files)
                {
                    var fileInfo = new FileInfo(filePath);

                    // Sprawdzenie limitów rozmiaru pliku
                    if (fileInfo.Length < rule.MinFileSizeBytes || fileInfo.Length > rule.MaxFileSizeBytes)
                        continue;

                    // Sprawdzenie rozszerzeń (jeśli podano)
                    if (rule.AllowedExtensions != null && rule.AllowedExtensions.Length > 0)
                    {
                        var fileExt = fileInfo.Extension.TrimStart('.').ToLowerInvariant();
                        var allowed = rule.AllowedExtensions.Select(e => e.TrimStart('.').Trim().ToLowerInvariant());
                        if (!allowed.Contains(fileExt))
                            continue;
                    }

                    string fileName = fileInfo.Name;
                    string destFilePath = Path.Combine(rule.DestinationPath, fileName);

                    // Obsługa konfliktów nazw
                    if (File.Exists(destFilePath))
                    {
                        switch (rule.ConflictStrategy)
                        {
                            case "Skip":
                                continue;

                            case "Overwrite":
                                File.Copy(filePath, destFilePath, true);
                                File.Delete(filePath);
                                movedForRule++;
                                continue;

                            case "AddTimestamp":
                            case "Timestamp (_yyyyMMdd_HHmmss)":
                                string nameNoExt = Path.GetFileNameWithoutExtension(fileName);
                                string ext = fileInfo.Extension;
                                destFilePath = Path.Combine(rule.DestinationPath, $"{nameNoExt}_{DateTime.Now:yyyyMMdd_HHmmss}{ext}");
                                break;

                            case "Custom Suffix":
                                string name = Path.GetFileNameWithoutExtension(fileName);
                                string extension = fileInfo.Extension;
                                string suffix = string.IsNullOrWhiteSpace(rule.CustomSuffix) ? "_copy" : rule.CustomSuffix;
                                destFilePath = Path.Combine(rule.DestinationPath, $"{name}{suffix}{extension}");
                                break;
                        }
                    }

                    File.Move(filePath, destFilePath);
                    movedForRule++;
                }

                totalFilesMoved += movedForRule;
                if (movedForRule > 0)
                {
                    newLogs.Add(new LogEntry
                    {
                        RuleName = rule.DisplayName,
                        Status = "OK",
                        FilesMoved = movedForRule,
                        Message = $"Przeniesiono pomyślnie {movedForRule} plików."
                    });
                }
            }
            catch (Exception ex)
            {
                newLogs.Add(new LogEntry
                {
                    RuleName = rule.DisplayName,
                    Status = "Error",
                    Message = $"Błąd: {ex.Message}"
                });
            }
        }

        config.LastRun = DateTime.Now;
        
        // Dodaj nowe logi na początek listy i przytnij do 100 wpisów
        if (newLogs.Count > 0)
        {
            config.Logs.InsertRange(0, newLogs);
            if (config.Logs.Count > 100)
                config.Logs = config.Logs.Take(100).ToList();
        }

        SaveConfig(config);
        return (totalFilesMoved, newLogs);
    }
}
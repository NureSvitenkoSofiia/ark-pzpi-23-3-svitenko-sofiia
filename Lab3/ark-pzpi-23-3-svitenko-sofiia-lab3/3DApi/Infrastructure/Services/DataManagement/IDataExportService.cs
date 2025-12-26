namespace _3DApi.Infrastructure.Services.DataManagement;

using Models;

/// <summary>
/// Service for exporting and backing up system data
/// </summary>
public interface IDataExportService
{
    /// <summary>
    /// Export all system data to JSON format
    /// </summary>
    Task<Result<string>> ExportAllDataToJsonAsync(string exportPath);

    /// <summary>
    /// Export specific entity data to JSON
    /// </summary>
    Task<Result<string>> ExportEntityDataAsync<T>(string exportPath) where T : Base;
}

public class BackupInfo
{
    public string BackupId { get; set; }
    public string BackupPath { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public long SizeInBytes { get; set; }
    public string Description { get; set; }
    public Dictionary<string, int> EntityCounts { get; set; }
}
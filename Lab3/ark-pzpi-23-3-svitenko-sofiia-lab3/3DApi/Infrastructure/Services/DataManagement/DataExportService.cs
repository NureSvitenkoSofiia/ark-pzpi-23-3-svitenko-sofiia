namespace _3DApi.Infrastructure.Services.DataManagement;

using DataAccess.Repo;
using Errors;
using Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO.Compression;
using _3DApi.Infrastructure.JsonConverters;

public class DataExportService : IDataExportService
{
    private readonly IGenericRepository<User> _userRepository;
    private readonly IGenericRepository<Printer> _printerRepository;
    private readonly IGenericRepository<Material> _materialRepository;
    private readonly IGenericRepository<PrintJob> _printJobRepository;
    private readonly IGenericRepository<PrinterMaterial> _printerMaterialRepository;
    private readonly ILogger<DataExportService> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Converters = { new IPAddressJsonConverter(), new IPAddressNullableJsonConverter() }
    };

    public DataExportService(
        IGenericRepository<User> userRepository,
        IGenericRepository<Printer> printerRepository,
        IGenericRepository<Material> materialRepository,
        IGenericRepository<PrintJob> printJobRepository,
        IGenericRepository<PrinterMaterial> printerMaterialRepository,
        ILogger<DataExportService> logger)
    {
        _userRepository = userRepository;
        _printerRepository = printerRepository;
        _materialRepository = materialRepository;
        _printJobRepository = printJobRepository;
        _printerMaterialRepository = printerMaterialRepository;
        _logger = logger;
    }

    public async Task<Result<string>> ExportAllDataToJsonAsync(string exportPath)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(exportPath) ?? ".");

            var exportData = new
            {
                ExportDate = DateTimeOffset.UtcNow,
                Version = "1.0",
                Users = await GetAllDataAsync(_userRepository),
                Printers = await GetAllDataAsync(_printerRepository),
                Materials = await GetAllDataAsync(_materialRepository),
                PrintJobs = await GetAllDataAsync(_printJobRepository),
                PrinterMaterials = await GetAllDataAsync(_printerMaterialRepository)
            };

            var json = JsonSerializer.Serialize(exportData, _jsonOptions);
            await File.WriteAllTextAsync(exportPath, json);

            _logger.LogInformation($"All data exported to {exportPath}");
            return Result<string>.Success(exportPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error exporting all data to {exportPath}");
            return Result<string>.Failure(
                Error.InternalServerError("export.FAILED", $"Failed to export data: {ex.Message}"));
        }
    }

    public async Task<Result<string>> ExportEntityDataAsync<T>(string exportPath) where T : Base
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(exportPath) ?? ".");

            var repository = GetRepository<T>();
            var data = await GetAllDataAsync(repository);

            var exportData = new
            {
                ExportDate = DateTimeOffset.UtcNow,
                EntityType = typeof(T).Name,
                Count = data.Count,
                Data = data
            };

            var json = JsonSerializer.Serialize(exportData, _jsonOptions);
            await File.WriteAllTextAsync(exportPath, json);

            _logger.LogInformation($"{typeof(T).Name} data exported to {exportPath}");
            return Result<string>.Success(exportPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error exporting {typeof(T).Name} data to {exportPath}");
            return Result<string>.Failure(
                Error.InternalServerError("export.ENTITY_FAILED", $"Failed to export {typeof(T).Name}: {ex.Message}"));
        }
    }

    private async Task<List<T>> GetAllDataAsync<T>(IGenericRepository<T> repository) where T : Base
    {
        var result = await repository.GetListByConditionAsync();
        return result.IsSuccess ? result.Value.ToList() : new List<T>();
    }

    private IGenericRepository<T> GetRepository<T>() where T : Base
    {
        return typeof(T).Name switch
        {
            nameof(User) => (IGenericRepository<T>)_userRepository,
            nameof(Printer) => (IGenericRepository<T>)_printerRepository,
            nameof(Material) => (IGenericRepository<T>)_materialRepository,
            nameof(PrintJob) => (IGenericRepository<T>)_printJobRepository,
            nameof(PrinterMaterial) => (IGenericRepository<T>)_printerMaterialRepository,
            _ => throw new ArgumentException($"Unknown entity type: {typeof(T).Name}")
        };
    }
}
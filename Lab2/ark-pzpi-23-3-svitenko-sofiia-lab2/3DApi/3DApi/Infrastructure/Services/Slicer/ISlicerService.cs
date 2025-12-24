namespace _3DApi.Infrastructure.Services.Slicer;

public interface ISlicerService
{
    Task<Result<string>> SliceAsync(string stlFilePath, int materialId, int printerId);
}


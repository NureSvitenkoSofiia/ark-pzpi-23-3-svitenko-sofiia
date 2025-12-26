namespace _3DApi.Models.DTOs;

public class FinishJobRequest
{
    public bool IsSuccess { get; set; }
    public double? ActualMaterialInGrams { get; set; }
    public string? ErrorMessage { get; set; }
}


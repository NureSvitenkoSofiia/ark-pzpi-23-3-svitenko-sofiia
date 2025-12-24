namespace _3DApi.Models.DTOs;

public class UpdateJobStatusRequest
{
    public string Status { get; set; }
    public string? ErrorMessage { get; set; }
    public double? ActualMaterialInGrams { get; set; }
}


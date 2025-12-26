namespace _3DApi.Models.DTOs;

public class UpdateMaterialRequest
{
    public string? Color { get; set; }
    public string? ColorHex { get; set; }
    public string? MaterialType { get; set; }
    public double? DensityInGramsPerCm3 { get; set; }
    public double? DiameterMm { get; set; }
}


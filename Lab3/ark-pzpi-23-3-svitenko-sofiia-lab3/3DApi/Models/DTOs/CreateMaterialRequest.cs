namespace _3DApi.Models.DTOs;

public class CreateMaterialRequest
{
    public string Color { get; set; }
    public string ColorHex { get; set; } // Format: "RRGGBB"
    public string MaterialType { get; set; }
    public double DensityInGramsPerCm3 { get; set; }
    public double DiameterMm { get; set; }
}
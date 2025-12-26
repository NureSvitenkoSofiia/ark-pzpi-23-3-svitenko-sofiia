namespace _3DApi.Models.DTOs;

public class MaterialManagementResponse
{
    public int Id { get; set; }
    public string Color { get; set; }
    public string ColorHex { get; set; }
    public string MaterialType { get; set; }
    public double DensityInGramsPerCm3 { get; set; }
    public double DiameterMm { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public int PrintersLoadedOn { get; set; }
    public double TotalUsageGrams { get; set; }
}
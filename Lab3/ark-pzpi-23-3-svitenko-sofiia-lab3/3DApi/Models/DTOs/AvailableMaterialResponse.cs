namespace _3DApi.Models.DTOs;

public class AvailableMaterialResponse
{
    public int Id { get; set; }
    public string Color { get; set; }
    public string ColorHex { get; set; }
    public string MaterialType { get; set; }
    public double DensityInGramsPerCm3 { get; set; }
    public double DiameterMm { get; set; }
    public List<PrinterMaterialInfo> AvailableOnPrinters { get; set; }
}

public class PrinterMaterialInfo
{
    public int PrinterId { get; set; }
    public string PrinterName { get; set; }
    public double QuantityInG { get; set; }
}


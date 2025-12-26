using System.Drawing;

namespace _3DApi.Models;

public class Material : Base
{
    public string Color { get; set; }
    
    public Color ColorHex { get; set; }
    
    public string MaterialType { get; set; }
    
    public double DensityInGramsPerCm3 { get; set; }
    
    public double DiameterMm { get; set; }
    
    public IEnumerable<PrinterMaterial> Printers { get; set; }
}
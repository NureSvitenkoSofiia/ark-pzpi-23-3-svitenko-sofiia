namespace _3DApi.Models;

public class PrinterMaterial : Base
{
    public int PrinterId { get; set; }
    
    public Printer Printer { get; set; }
    
    public int MaterialId { get; set; }
    
    public Material Material { get; set; }
    
    public double QuantityInG { get; set; }
}
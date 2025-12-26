namespace _3DApi.Models;

public class PrintJob : Base
{
    public string StlFilePath { get; set; }
    
    public string GCodeFilePath { get; set; }
    
    public int RequiredMaterialId { get; set; }
    
    public Material RequiredMaterial { get; set; }
    
    public double EstimatedMaterialInGrams { get; set; }
    
    public double ActualMaterialInGrams { get; set; }
    
    public double EstimatedPrintTimeMinutes { get; set; }
    
    public int PrinterId { get; set; }
    
    public Printer Printer { get; set; }
    
    public string Status { get; set; }
    
    public string? ErrorMessage { get; set; }
    
    public DateTimeOffset? StartedAt { get; set; }
    
    public DateTimeOffset? CompletedAt { get; set; }
    
    public int RequesterId { get; set; }
    
    public User Requester { get; set; }
}
namespace _3DApi.Models.DTOs;

public class PrintJobManagementResponse
{
    public int Id { get; set; }
    public int RequesterId { get; set; }
    public string RequesterEmail { get; set; }
    public int PrinterId { get; set; }
    public string PrinterName { get; set; }
    public string MaterialType { get; set; }
    public string MaterialColor { get; set; }
    public double EstimatedMaterialInGrams { get; set; }
    public double ActualMaterialInGrams { get; set; }
    public double EstimatedPrintTimeMinutes { get; set; }
    public string Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string StlFilePath { get; set; }
    public string GCodeFilePath { get; set; }
}


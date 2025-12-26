namespace _3DApi.Models.DTOs;

public class PrintJobResponse
{
    public int Id { get; set; }
    public string StlFilePath { get; set; }
    public string? GCodeFilePath { get; set; }
    public MaterialInfo RequiredMaterial { get; set; }
    public double EstimatedMaterialInGrams { get; set; }
    public double ActualMaterialInGrams { get; set; }
    public double EstimatedPrintTimeMinutes { get; set; }
    public PrinterInfo Printer { get; set; }
    public string Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public class MaterialInfo
{
    public int Id { get; set; }
    public string Color { get; set; }
    public string MaterialType { get; set; }
}

public class PrinterInfo
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Status { get; set; }
}


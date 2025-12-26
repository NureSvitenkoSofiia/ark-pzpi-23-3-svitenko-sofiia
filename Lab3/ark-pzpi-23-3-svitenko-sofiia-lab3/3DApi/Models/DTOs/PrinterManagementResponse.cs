namespace _3DApi.Models.DTOs;

public class PrinterManagementResponse
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Status { get; set; }
    public string IpAddress { get; set; }
    public DateTimeOffset LastPing { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public List<PrinterMaterialDetails> LoadedMaterials { get; set; }
    public int PendingJobsCount { get; set; }
    public int CompletedJobsCount { get; set; }
}

public class PrinterMaterialDetails
{
    public int MaterialId { get; set; }
    public string MaterialType { get; set; }
    public string Color { get; set; }
    public double QuantityInG { get; set; }
}
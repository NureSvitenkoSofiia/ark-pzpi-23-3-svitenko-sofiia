namespace _3DApi.Models.DTOs;

public class JobQueueItemResponse
{
    public int Id { get; set; }
    public string StlFilePath { get; set; }
    public string? GCodeFilePath { get; set; }
    public MaterialResponse RequiredMaterial { get; set; }
    public double EstimatedMaterialInGrams { get; set; }
    public string Status { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}


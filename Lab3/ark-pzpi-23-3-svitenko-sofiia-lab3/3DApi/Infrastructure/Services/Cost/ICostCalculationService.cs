namespace _3DApi.Infrastructure.Services.Cost;

using Models;

/// <summary>
/// Service for calculating costs related to 3D printing operations
/// Mathematical methods used: Cost optimization, linear pricing models, resource allocation
/// </summary>
public interface ICostCalculationService
{
    /// <summary>
    /// Calculate the total cost of a print job including material, electricity, and maintenance costs
    /// </summary>
    Task<Result<PrintJobCostBreakdown>> CalculatePrintJobCostAsync(
        double materialInGrams,
        double printTimeMinutes,
        string materialType);

    /// <summary>
    /// Calculate cost per gram for a specific material type
    /// </summary>
    double CalculateMaterialCostPerGram(string materialType);

    /// <summary>
    /// Calculate electricity cost based on print time and printer power consumption
    /// </summary>
    double CalculateElectricityCost(double printTimeMinutes, double printerWattage);

    /// <summary>
    /// Calculate maintenance cost allocation per print based on printer usage
    /// </summary>
    double CalculateMaintenanceCost(double printTimeMinutes);
    
    /// <summary>
    /// Calculate optimal pricing for customer orders with profit margin
    /// </summary>
    Task<Result<PricingRecommendation>> CalculateOptimalPricingAsync(
        int printJobId,
        double targetProfitMarginPercent);
}

public class PrintJobCostBreakdown
{
    public double MaterialCost { get; set; }
    public double ElectricityCost { get; set; }
    public double MaintenanceCost { get; set; }
    public double TotalCost { get; set; }
    public double MaterialUsedGrams { get; set; }
    public double PrintTimeMinutes { get; set; }
    public string MaterialType { get; set; }
}

public class PricingRecommendation
{
    public double TotalCost { get; set; }
    public double SuggestedPrice { get; set; }
    public double ProfitMargin { get; set; }
    public double ProfitAmount { get; set; }
    public PrintJobCostBreakdown CostBreakdown { get; set; }
}


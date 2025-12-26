namespace _3DApi.Infrastructure.Services.Cost;

using DataAccess.Repo;
using Errors;
using Models;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Implementation of cost calculation service using mathematical models
/// 
/// Mathematical Methods Justification:
/// 1. Linear Cost Model: Material cost = cost_per_gram * grams_used
///    - Simple, accurate for direct material consumption
///    - Industry standard for material costing
/// 
/// 2. Time-based Cost Allocation: Uses linear interpolation for electricity and maintenance
///    - Electricity: Power (W) * Time (h) * Rate ($/kWh) / 1000
///    - Maintenance: Amortized cost per hour of operation
/// 
/// 3. Profit Margin Optimization: Price = Cost / (1 - Margin%)
///    - Standard business pricing formula
///    - Ensures target profit while covering all costs
/// </summary>
public class CostCalculationService : ICostCalculationService
{
    private readonly IGenericRepository<PrintJob> _printJobRepository;
    private readonly ILogger<CostCalculationService> _logger;
    private readonly IConfiguration _configuration;

    // Cost constants (configurable via appsettings)
    private const double DEFAULT_PLA_COST_PER_KG = 20.0; // USD per kg
    private const double DEFAULT_ABS_COST_PER_KG = 25.0;
    private const double DEFAULT_PETG_COST_PER_KG = 30.0;
    private const double DEFAULT_ELECTRICITY_RATE = 0.12; // USD per kWh
    private const double DEFAULT_PRINTER_WATTAGE = 200.0; // Watts
    private const double DEFAULT_MAINTENANCE_COST_PER_HOUR = 0.50; // USD per hour of operation

    public CostCalculationService(
        IGenericRepository<PrintJob> printJobRepository,
        ILogger<CostCalculationService> logger,
        IConfiguration configuration)
    {
        _printJobRepository = printJobRepository;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<Result<PrintJobCostBreakdown>> CalculatePrintJobCostAsync(
        double materialInGrams,
        double printTimeMinutes,
        string materialType)
    {
        try
        {
            // 1. Calculate material cost using linear pricing model
            var materialCostPerGram = CalculateMaterialCostPerGram(materialType);
            var materialCost = materialCostPerGram * materialInGrams;

            // 2. Calculate electricity cost using power consumption formula
            var printerWattage = _configuration.GetValue<double>("Costing:PrinterWattage", DEFAULT_PRINTER_WATTAGE);
            var electricityCost = CalculateElectricityCost(printTimeMinutes, printerWattage);

            // 3. Calculate maintenance cost allocation
            var maintenanceCost = CalculateMaintenanceCost(printTimeMinutes);

            // 4. Total cost calculation
            var totalCost = materialCost + electricityCost + maintenanceCost;

            var breakdown = new PrintJobCostBreakdown
            {
                MaterialCost = Math.Round(materialCost, 2),
                ElectricityCost = Math.Round(electricityCost, 2),
                MaintenanceCost = Math.Round(maintenanceCost, 2),
                TotalCost = Math.Round(totalCost, 2),
                MaterialUsedGrams = Math.Round(materialInGrams, 2),
                PrintTimeMinutes = Math.Round(printTimeMinutes, 2),
                MaterialType = materialType
            };

            _logger.LogInformation(
                $"Cost calculation: Material={breakdown.MaterialCost:C}, " +
                $"Electricity={breakdown.ElectricityCost:C}, " +
                $"Maintenance={breakdown.MaintenanceCost:C}, " +
                $"Total={breakdown.TotalCost:C}");

            return Result<PrintJobCostBreakdown>.Success(breakdown);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating print job cost");
            return Result<PrintJobCostBreakdown>.Failure(
                Error.InternalServerError("cost.CALCULATION_FAILED", $"Failed to calculate cost: {ex.Message}"));
        }
    }

    public double CalculateMaterialCostPerGram(string materialType)
    {
        // Get cost per kg from configuration, then convert to cost per gram
        var costPerKg = materialType.ToUpper() switch
        {
            "PLA" => _configuration.GetValue<double>("Costing:PlaCostPerKg", DEFAULT_PLA_COST_PER_KG),
            "ABS" => _configuration.GetValue<double>("Costing:AbsCostPerKg", DEFAULT_ABS_COST_PER_KG),
            "PETG" => _configuration.GetValue<double>("Costing:PetgCostPerKg", DEFAULT_PETG_COST_PER_KG),
            _ => _configuration.GetValue<double>("Costing:DefaultCostPerKg", DEFAULT_PLA_COST_PER_KG)
        };

        return costPerKg / 1000.0; // Convert kg to grams
    }

    public double CalculateElectricityCost(double printTimeMinutes, double printerWattage)
    {
        // Formula: Cost = (Power in W × Time in hours × Rate per kWh) / 1000
        var electricityRate = _configuration.GetValue<double>("Costing:ElectricityRate", DEFAULT_ELECTRICITY_RATE);
        var timeInHours = printTimeMinutes / 60.0;
        var energyKwh = (printerWattage * timeInHours) / 1000.0;
        return energyKwh * electricityRate;
    }

    public double CalculateMaintenanceCost(double printTimeMinutes)
    {
        // Amortized maintenance cost based on operational time
        // Formula: Cost = (Maintenance rate per hour × Time in hours)
        var maintenanceRate = _configuration.GetValue<double>("Costing:MaintenanceCostPerHour", DEFAULT_MAINTENANCE_COST_PER_HOUR);
        var timeInHours = printTimeMinutes / 60.0;
        return maintenanceRate * timeInHours;
    }

    public async Task<Result<PricingRecommendation>> CalculateOptimalPricingAsync(
        int printJobId,
        double targetProfitMarginPercent)
    {
        try
        {
            // Validate profit margin
            if (targetProfitMarginPercent < 0 || targetProfitMarginPercent > 100)
            {
                return Result<PricingRecommendation>.Failure(
                    Error.Validation("pricing.INVALID_MARGIN", "Profit margin must be between 0 and 100 percent"));
            }

            // Get print job details
            var jobResult = await _printJobRepository.GetSingleByConditionAsync(
                pj => pj.Id == printJobId,
                includes: [q => q.Include(pj => pj.RequiredMaterial)]);

            if (!jobResult.IsSuccess)
            {
                return Result<PricingRecommendation>.Failure(
                    Error.NotFound("printjob.NOT_FOUND", "Print job not found"));
            }

            var job = jobResult.Value;

            // Calculate cost breakdown
            var costResult = await CalculatePrintJobCostAsync(
                job.EstimatedMaterialInGrams,
                job.EstimatedPrintTimeMinutes,
                job.RequiredMaterial.MaterialType);

            if (!costResult.IsSuccess)
            {
                return Result<PricingRecommendation>.Failure(costResult.Errors);
            }

            var costBreakdown = costResult.Value;

            // Calculate optimal price using profit margin formula
            // Price = Cost / (1 - Margin%)
            var marginDecimal = targetProfitMarginPercent / 100.0;
            var suggestedPrice = costBreakdown.TotalCost / (1 - marginDecimal);
            var profitAmount = suggestedPrice - costBreakdown.TotalCost;

            var recommendation = new PricingRecommendation
            {
                TotalCost = Math.Round(costBreakdown.TotalCost, 2),
                SuggestedPrice = Math.Round(suggestedPrice, 2),
                ProfitMargin = targetProfitMarginPercent,
                ProfitAmount = Math.Round(profitAmount, 2),
                CostBreakdown = costBreakdown
            };

            _logger.LogInformation(
                $"Pricing recommendation for Job {printJobId}: " +
                $"Cost={recommendation.TotalCost:C}, " +
                $"Price={recommendation.SuggestedPrice:C}, " +
                $"Profit={recommendation.ProfitAmount:C} ({targetProfitMarginPercent}%)");

            return Result<PricingRecommendation>.Success(recommendation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error calculating optimal pricing for job {printJobId}");
            return Result<PricingRecommendation>.Failure(
                Error.InternalServerError("pricing.CALCULATION_FAILED", $"Failed to calculate pricing: {ex.Message}"));
        }
    }
}
using _3DApi.Infrastructure.ApiResult;
using _3DApi.Infrastructure.Services.PrintJob;
using _3DApi.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace _3DApi.ApiControllers;

[ApiController]
[Route("api/[controller]")]
public class PrintJobsController : ControllerBase
{
    private readonly IPrintJobService _printJobService;

    public PrintJobsController(IPrintJobService printJobService)
    {
        _printJobService = printJobService;
    }
    
    [HttpGet("materials")]
    public async Task<IActionResult> GetAvailableMaterials()
    {
        var result = await _printJobService.GetAvailableMaterialsAsync();
        return result.Match(StatusCodes.Status200OK);
    }

    public class PrintJobDto
    {
        public int UserId { get; set; }
        
        public int PrinterId { get; set; }
        
        public int RequiredMaterialId { get; set; }
        
        public IFormFile? StlFile { get; set; }
    }
    
    [HttpPost]
    public async Task<IActionResult> CreatePrintJob([FromForm] PrintJobDto dto)
    {
        var request = new CreatePrintJobRequest
        {
            RequiredMaterialId = dto.RequiredMaterialId,
            PrinterId = dto.PrinterId
        };

        var result = await _printJobService.CreatePrintJobAsync(dto.UserId, request, dto.StlFile);
        return result.Match(StatusCodes.Status201Created);
    }
}


using MeatPro.Services;
using Microsoft.AspNetCore.Mvc;

namespace MeatPro.Controllers.Api;

[Route("api/stock")]
public sealed class StockController : ApiController
{
    private readonly IStockService _stockService;

    public StockController(IStockService stockService)
    {
        _stockService = stockService;
    }

    [HttpGet("movements")]
    public async Task<IActionResult> GetMovements(int count = 20, CancellationToken cancellationToken = default)
    {
        return Ok(await _stockService.GetRecentMovementsAsync(count, cancellationToken));
    }

    [HttpPost("raw-materials/{id}/stock-in")]
    public async Task<IActionResult> StockIn(int id, [FromBody] StockInRequest request, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var (success, message) = await _stockService.StockInRawMaterialAsync(id, request.Quantity, request.ReferenceNumber, request.Notes, request.PerformedBy, cancellationToken);
        return success ? Ok(new { id, message }) : Error(message);
    }

    [HttpPost("raw-materials/{id}/stock-out")]
    public async Task<IActionResult> StockOut(int id, [FromBody] StockOutRequest request, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var (success, message) = await _stockService.StockOutRawMaterialAsync(id, request.Quantity, request.ReferenceNumber, request.Notes, request.PerformedBy, cancellationToken);
        return success ? Ok(new { id, message }) : Error(message);
    }

    [HttpPost("raw-materials/{id}/release-to-production")]
    public async Task<IActionResult> ReleaseToProduction(int id, [FromBody] ReleaseRequest request, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var (success, message) = await _stockService.ReleaseToProductionAsync(id, request.Quantity, request.WorkOrderNumber, request.PerformedBy, cancellationToken);
        return success ? Ok(new { id, message }) : Error(message);
    }

    [HttpPost("finished-goods/{id}/adjust")]
    public async Task<IActionResult> Adjust(int id, [FromBody] AdjustRequest request, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var (success, message) = await _stockService.AdjustFinishedGoodAsync(id, request.NewQuantity, request.Reason, request.PerformedBy, cancellationToken);
        return success ? Ok(new { id, message }) : Error(message);
    }
}

public sealed class StockInRequest
{
    public decimal Quantity { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
}

public sealed class StockOutRequest
{
    public decimal Quantity { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
}

public sealed class ReleaseRequest
{
    public decimal Quantity { get; set; }
    public string WorkOrderNumber { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = string.Empty;
}

public sealed class AdjustRequest
{
    public decimal NewQuantity { get; set; }
    public string? Reason { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
}

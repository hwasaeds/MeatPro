using System.Text;
using MeatPro.Services;
using Microsoft.AspNetCore.Mvc;

namespace MeatPro.Controllers.Api;

[Route("api/reports")]
public sealed class ReportsApiController : ApiController
{
    private readonly IReportService _reportService;

    public ReportsApiController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("inventory")]
    public async Task<IActionResult> GetInventoryReport(DateTime? dateFrom, DateTime? dateTo, string? statusFilter, CancellationToken cancellationToken = default)
    {
        return Ok(await _reportService.BuildInventoryReportAsync(dateFrom, dateTo, statusFilter, cancellationToken));
    }

    [HttpGet("production")]
    public async Task<IActionResult> GetProductionReport(DateTime? dateFrom, DateTime? dateTo, string? statusFilter, CancellationToken cancellationToken = default)
    {
        return Ok(await _reportService.BuildProductionReportAsync(dateFrom, dateTo, statusFilter, cancellationToken));
    }

    [HttpGet("procurement")]
    public async Task<IActionResult> GetProcurementReport(DateTime? dateFrom, DateTime? dateTo, string? statusFilter, CancellationToken cancellationToken = default)
    {
        return Ok(await _reportService.BuildProcurementReportAsync(dateFrom, dateTo, statusFilter, cancellationToken));
    }

    [HttpGet("inventory/csv")]
    public async Task<IActionResult> ExportInventoryCsv(DateTime? dateFrom, DateTime? dateTo, string? statusFilter, CancellationToken cancellationToken = default)
    {
        var csv = await _reportService.ExportInventoryCsvAsync(dateFrom, dateTo, statusFilter, cancellationToken);
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"inventory-report-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet("production/csv")]
    public async Task<IActionResult> ExportProductionCsv(DateTime? dateFrom, DateTime? dateTo, string? statusFilter, CancellationToken cancellationToken = default)
    {
        var csv = await _reportService.ExportProductionCsvAsync(dateFrom, dateTo, statusFilter, cancellationToken);
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"production-report-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet("procurement/csv")]
    public async Task<IActionResult> ExportProcurementCsv(DateTime? dateFrom, DateTime? dateTo, string? statusFilter, CancellationToken cancellationToken = default)
    {
        var csv = await _reportService.ExportProcurementCsvAsync(dateFrom, dateTo, statusFilter, cancellationToken);
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"procurement-report-{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}

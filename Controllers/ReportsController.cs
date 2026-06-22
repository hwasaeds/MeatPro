using System.Text;
using MeatPro.Services;
using MeatPro.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeatPro.Controllers;

[Authorize]
public sealed class ReportsController : Controller
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> Inventory(DateTime? dateFrom, DateTime? dateTo, string? statusFilter, CancellationToken cancellationToken = default)
    {
        var model = await _reportService.BuildInventoryReportAsync(dateFrom, dateTo, statusFilter, cancellationToken);
        return View(model);
    }

    public async Task<IActionResult> Production(DateTime? dateFrom, DateTime? dateTo, string? statusFilter, CancellationToken cancellationToken = default)
    {
        var model = await _reportService.BuildProductionReportAsync(dateFrom, dateTo, statusFilter, cancellationToken);
        return View(model);
    }

    public async Task<IActionResult> Procurement(DateTime? dateFrom, DateTime? dateTo, string? statusFilter, CancellationToken cancellationToken = default)
    {
        var model = await _reportService.BuildProcurementReportAsync(dateFrom, dateTo, statusFilter, cancellationToken);
        return View(model);
    }

    public async Task<IActionResult> ExportInventoryCsv(DateTime? dateFrom, DateTime? dateTo, string? statusFilter, CancellationToken cancellationToken = default)
    {
        var csv = await _reportService.ExportInventoryCsvAsync(dateFrom, dateTo, statusFilter, cancellationToken);
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"inventory-report-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    public async Task<IActionResult> ExportProductionCsv(DateTime? dateFrom, DateTime? dateTo, string? statusFilter, CancellationToken cancellationToken = default)
    {
        var csv = await _reportService.ExportProductionCsvAsync(dateFrom, dateTo, statusFilter, cancellationToken);
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"production-report-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    public async Task<IActionResult> ExportProcurementCsv(DateTime? dateFrom, DateTime? dateTo, string? statusFilter, CancellationToken cancellationToken = default)
    {
        var csv = await _reportService.ExportProcurementCsvAsync(dateFrom, dateTo, statusFilter, cancellationToken);
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"procurement-report-{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
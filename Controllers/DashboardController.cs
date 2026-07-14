using MeatPro.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeatPro.Controllers;

[Authorize]
public sealed class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IAlertService _alertService;

    public DashboardController(IDashboardService dashboardService, IAlertService alertService)
    {
        _dashboardService = dashboardService;
        _alertService = alertService;
    }

    public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        await _alertService.GenerateAlertsAsync(cancellationToken);
        var model = await _dashboardService.BuildAsync(fromDate, toDate, cancellationToken);
        return View("Overview", model);
    }
}
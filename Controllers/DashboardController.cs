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

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        await _alertService.GenerateAlertsAsync(cancellationToken);
        var model = await _dashboardService.BuildAsync(cancellationToken);
        return View("Overview", model);
    }
}
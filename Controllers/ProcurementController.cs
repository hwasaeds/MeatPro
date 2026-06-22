using MeatPro.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeatPro.Controllers;

[Authorize]
public sealed class ProcurementController : Controller
{
    private readonly IProcurementService _procurementService;

    public ProcurementController(IProcurementService procurementService)
    {
        _procurementService = procurementService;
    }

    public async Task<IActionResult> Suppliers(CancellationToken cancellationToken) => View("~/Views/Shared/ModuleIndex.cshtml", await _procurementService.BuildSuppliersPageAsync(cancellationToken));
    public async Task<IActionResult> Purchases(CancellationToken cancellationToken) => View("~/Views/Shared/ModuleIndex.cshtml", await _procurementService.BuildPurchasesPageAsync(cancellationToken));
}
using MeatPro.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeatPro.Controllers;

[Authorize]
public sealed class ProductionController : Controller
{
    private readonly IProductionService _productionService;

    public ProductionController(IProductionService productionService)
    {
        _productionService = productionService;
    }

    public async Task<IActionResult> Plans(CancellationToken cancellationToken) => View("~/Views/Shared/ModuleIndex.cshtml", await _productionService.BuildPlansPageAsync(cancellationToken));
    public async Task<IActionResult> WorkOrders(CancellationToken cancellationToken) => View("~/Views/Shared/ModuleIndex.cshtml", await _productionService.BuildWorkOrdersPageAsync(cancellationToken));
    public async Task<IActionResult> Traceability(CancellationToken cancellationToken) => View("~/Views/Shared/ModuleIndex.cshtml", await _productionService.BuildTraceabilityPageAsync(cancellationToken));
}
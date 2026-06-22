using MeatPro.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeatPro.Controllers;

[Authorize]
public sealed class InventoryController : Controller
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public IActionResult RawMaterials() => RedirectToAction("Index", "RawMaterials");
    public async Task<IActionResult> FinishedGoods(CancellationToken cancellationToken) => View("~/Views/Shared/ModuleIndex.cshtml", await _inventoryService.BuildFinishedGoodsPageAsync(cancellationToken));
    public async Task<IActionResult> StockMovements(CancellationToken cancellationToken) => View("~/Views/Shared/ModuleIndex.cshtml", await _inventoryService.BuildStockMovementsPageAsync(cancellationToken));
}
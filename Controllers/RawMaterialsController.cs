using System.Security.Claims;
using MeatPro.Services;
using MeatPro.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MeatPro.Controllers;

[Authorize(Roles = "System Administrator,Inventory Personnel")]
public sealed class RawMaterialsController : Controller
{
    private readonly IRawMaterialService _rawMaterialService;
    private readonly IStockService _stockService;

    public RawMaterialsController(IRawMaterialService rawMaterialService, IStockService stockService)
    {
        _rawMaterialService = rawMaterialService;
        _stockService = stockService;
    }

    private string CurrentUser => User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email) ?? "unknown";

    public async Task<IActionResult> Index(string? search, string? sort, int page = 1, CancellationToken cancellationToken = default)
    {
        var model = await _rawMaterialService.BuildIndexAsync(search, sort, page, 10, cancellationToken);
        return View(model);
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
    {
        var model = await _rawMaterialService.GetDetailsAsync(id, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    public IActionResult Create()
    {
        return View(new RawMaterialFormViewModel
        {
            UnitOfMeasure = "kg",
            IsActive = true
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RawMaterialFormViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await _rawMaterialService.IsSkuInUseAsync(model.SKU, null, cancellationToken))
        {
            ModelState.AddModelError(nameof(RawMaterialFormViewModel.SKU), "That SKU is already in use.");
            return View(model);
        }

        try
        {
            var id = await _rawMaterialService.CreateAsync(model, cancellationToken);
            TempData["SuccessMessage"] = $"Raw material {model.Name} was created successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "The raw material could not be saved. Please check the values and try again.");
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
    {
        var model = await _rawMaterialService.GetEditAsync(id, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RawMaterialFormViewModel model, CancellationToken cancellationToken = default)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await _rawMaterialService.IsSkuInUseAsync(model.SKU, id, cancellationToken))
        {
            ModelState.AddModelError(nameof(RawMaterialFormViewModel.SKU), "That SKU is already in use.");
            return View(model);
        }

        try
        {
            var updated = await _rawMaterialService.UpdateAsync(id, model, cancellationToken);
            if (!updated)
            {
                return NotFound();
            }

            TempData["SuccessMessage"] = $"Raw material {model.Name} was updated successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "The raw material could not be updated. Please check the values and try again.");
            return View(model);
        }
    }

    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var model = await _rawMaterialService.GetDeleteAsync(id, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken = default)
    {
        var deleted = await _rawMaterialService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        TempData["SuccessMessage"] = "Raw material deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> StockIn(int id, CancellationToken cancellationToken = default)
    {
        var model = await _rawMaterialService.GetDetailsAsync(id, cancellationToken);
        if (model is null) return NotFound();
        return View(new StockOperationViewModel { RawMaterialId = id, RawMaterialName = model.Name, UnitOfMeasure = model.UnitOfMeasure, CurrentStock = model.CurrentStock });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StockIn(int id, StockOperationViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return View(model);

        var (success, message) = await _stockService.StockInRawMaterialAsync(id, model.Quantity, model.ReferenceNumber, model.Notes, CurrentUser, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            return View(model);
        }

        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> StockOut(int id, CancellationToken cancellationToken = default)
    {
        var model = await _rawMaterialService.GetDetailsAsync(id, cancellationToken);
        if (model is null) return NotFound();
        return View(new StockOperationViewModel { RawMaterialId = id, RawMaterialName = model.Name, UnitOfMeasure = model.UnitOfMeasure, CurrentStock = model.CurrentStock });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StockOut(int id, StockOperationViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return View(model);

        var (success, message) = await _stockService.StockOutRawMaterialAsync(id, model.Quantity, model.ReferenceNumber, model.Notes, CurrentUser, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            return View(model);
        }

        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> ReleaseToProduction(int id, CancellationToken cancellationToken = default)
    {
        var model = await _rawMaterialService.GetDetailsAsync(id, cancellationToken);
        if (model is null) return NotFound();
        return View(new ReleaseToProductionViewModel { RawMaterialId = id, RawMaterialName = model.Name, UnitOfMeasure = model.UnitOfMeasure, CurrentStock = model.CurrentStock });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReleaseToProduction(int id, ReleaseToProductionViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return View(model);

        var (success, message) = await _stockService.ReleaseToProductionAsync(id, model.Quantity, model.WorkOrderNumber, CurrentUser, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            return View(model);
        }

        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }
}

using System.Security.Claims;
using MeatPro.Services;
using MeatPro.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MeatPro.Controllers;

[Authorize(Roles = "System Administrator,Inventory Personnel")]
public sealed class FinishedGoodsController : Controller
{
    private readonly IFinishedGoodService _finishedGoodService;
    private readonly IStockService _stockService;

    public FinishedGoodsController(IFinishedGoodService finishedGoodService, IStockService stockService)
    {
        _finishedGoodService = finishedGoodService;
        _stockService = stockService;
    }

    private string CurrentUser => User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email) ?? "unknown";

    public async Task<IActionResult> Index(string? search, string? sort, int page = 1, CancellationToken cancellationToken = default)
    {
        var model = await _finishedGoodService.BuildIndexAsync(search, sort, page, 10, cancellationToken);
        return View(model);
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
    {
        var model = await _finishedGoodService.GetDetailsAsync(id, cancellationToken);
        if (model is null) return NotFound();
        return View(model);
    }

    public IActionResult Create()
    {
        return View(new FinishedGoodFormViewModel
        {
            IsActive = true
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FinishedGoodFormViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return View(model);

        if (await _finishedGoodService.IsSkuInUseAsync(model.SKU, null, cancellationToken))
        {
            ModelState.AddModelError(nameof(FinishedGoodFormViewModel.SKU), "That SKU is already in use.");
            return View(model);
        }

        try
        {
            var id = await _finishedGoodService.CreateAsync(model, cancellationToken);
            TempData["SuccessMessage"] = $"Finished good {model.Name} was created successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "The finished good could not be saved. Please check the values and try again.");
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
    {
        var model = await _finishedGoodService.GetEditAsync(id, cancellationToken);
        if (model is null) return NotFound();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FinishedGoodFormViewModel model, CancellationToken cancellationToken = default)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid) return View(model);

        if (await _finishedGoodService.IsSkuInUseAsync(model.SKU, id, cancellationToken))
        {
            ModelState.AddModelError(nameof(FinishedGoodFormViewModel.SKU), "That SKU is already in use.");
            return View(model);
        }

        try
        {
            var updated = await _finishedGoodService.UpdateAsync(id, model, cancellationToken);
            if (!updated) return NotFound();

            TempData["SuccessMessage"] = $"Finished good {model.Name} was updated successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "The finished good could not be updated. Please check the values and try again.");
            return View(model);
        }
    }

    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var model = await _finishedGoodService.GetDeleteAsync(id, cancellationToken);
        if (model is null) return NotFound();
        return View(model);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken = default)
    {
        var deleted = await _finishedGoodService.DeleteAsync(id, cancellationToken);
        if (!deleted) return NotFound();

        TempData["SuccessMessage"] = "Finished good deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Adjust(int id, CancellationToken cancellationToken = default)
    {
        var model = await _finishedGoodService.GetDetailsAsync(id, cancellationToken);
        if (model is null) return NotFound();
        return View(new FinishedGoodAdjustmentViewModel { FinishedGoodId = id, FinishedGoodName = model.Name, CurrentStock = model.CurrentStock, NewQuantity = model.CurrentStock });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Adjust(int id, FinishedGoodAdjustmentViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return View(model);

        var (success, message) = await _stockService.AdjustFinishedGoodAsync(id, model.NewQuantity, model.Reason, CurrentUser, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            return View(model);
        }

        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }
}

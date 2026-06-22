using MeatPro.Services;
using MeatPro.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MeatPro.Controllers;

[Authorize]
public sealed class SuppliersController : Controller
{
    private readonly ISupplierService _supplierService;

    public SuppliersController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    public async Task<IActionResult> Index(string? search, string? sort, int page = 1, CancellationToken cancellationToken = default)
    {
        var model = await _supplierService.BuildIndexAsync(search, sort, page, 10, cancellationToken);
        return View(model);
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
    {
        var model = await _supplierService.GetDetailsAsync(id, cancellationToken);
        if (model is null) return NotFound();
        return View(model);
    }

    public IActionResult Create()
    {
        return View(new SupplierFormViewModel { IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SupplierFormViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return View(model);

        if (await _supplierService.IsNameInUseAsync(model.Name, null, cancellationToken))
        {
            ModelState.AddModelError(nameof(SupplierFormViewModel.Name), "That supplier name is already in use.");
            return View(model);
        }

        try
        {
            var id = await _supplierService.CreateAsync(model, cancellationToken);
            TempData["SuccessMessage"] = $"Supplier {model.Name} was created successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "The supplier could not be saved. Please check the values and try again.");
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
    {
        var model = await _supplierService.GetEditAsync(id, cancellationToken);
        if (model is null) return NotFound();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SupplierFormViewModel model, CancellationToken cancellationToken = default)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid) return View(model);

        if (await _supplierService.IsNameInUseAsync(model.Name, id, cancellationToken))
        {
            ModelState.AddModelError(nameof(SupplierFormViewModel.Name), "That supplier name is already in use.");
            return View(model);
        }

        try
        {
            var updated = await _supplierService.UpdateAsync(id, model, cancellationToken);
            if (!updated) return NotFound();

            TempData["SuccessMessage"] = $"Supplier {model.Name} was updated successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "The supplier could not be updated. Please check the values and try again.");
            return View(model);
        }
    }

    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var model = await _supplierService.GetDeleteAsync(id, cancellationToken);
        if (model is null) return NotFound();
        return View(model);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken = default)
    {
        var deleted = await _supplierService.DeleteAsync(id, cancellationToken);
        if (!deleted) return NotFound();

        TempData["SuccessMessage"] = "Supplier deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}

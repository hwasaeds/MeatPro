using MeatPro.Data;
using MeatPro.Models;
using MeatPro.Services;
using MeatPro.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MeatPro.Controllers;

[Authorize]
public sealed class ProductionPlansController : Controller
{
    private readonly IProductionPlanService _productionPlanService;
    private readonly ApplicationDbContext _context;

    public ProductionPlansController(IProductionPlanService productionPlanService, ApplicationDbContext context)
    {
        _productionPlanService = productionPlanService;
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, string? sort, string? statusFilter, int page = 1, CancellationToken cancellationToken = default)
    {
        var model = await _productionPlanService.BuildIndexAsync(search, sort, statusFilter, page, 10, cancellationToken);
        return View(model);
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
    {
        var model = await _productionPlanService.GetDetailsAsync(id, cancellationToken);
        if (model is null) return NotFound();
        return View(model);
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
    {
        var products = await GetProductOptionsAsync(cancellationToken);
        return View(new ProductionPlanFormViewModel
        {
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(7),
            Status = ProductionPlanStatus.Draft,
            Products = products
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductionPlanFormViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            model.Products = await GetProductOptionsAsync(cancellationToken);
            return View(model);
        }

        if (await _productionPlanService.IsPlanCodeInUseAsync(model.PlanCode, null, cancellationToken))
        {
            ModelState.AddModelError(nameof(ProductionPlanFormViewModel.PlanCode), "That plan code is already in use.");
            model.Products = await GetProductOptionsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var id = await _productionPlanService.CreateAsync(model, cancellationToken);
            TempData["SuccessMessage"] = $"Production plan {model.PlanCode} was created successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "The plan could not be saved. Please check the values and try again.");
            model.Products = await GetProductOptionsAsync(cancellationToken);
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
    {
        var model = await _productionPlanService.GetEditAsync(id, cancellationToken);
        if (model is null) return NotFound();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductionPlanFormViewModel model, CancellationToken cancellationToken = default)
    {
        if (id != model.Id) return NotFound();

        if (!ModelState.IsValid)
        {
            model.Products = await GetProductOptionsAsync(cancellationToken);
            return View(model);
        }

        if (await _productionPlanService.IsPlanCodeInUseAsync(model.PlanCode, id, cancellationToken))
        {
            ModelState.AddModelError(nameof(ProductionPlanFormViewModel.PlanCode), "That plan code is already in use.");
            model.Products = await GetProductOptionsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var updated = await _productionPlanService.UpdateAsync(id, model, cancellationToken);
            if (!updated) return NotFound();

            TempData["SuccessMessage"] = $"Production plan {model.PlanCode} was updated successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "The plan could not be updated. Please check the values and try again.");
            model.Products = await GetProductOptionsAsync(cancellationToken);
            return View(model);
        }
    }

    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var model = await _productionPlanService.GetDeleteAsync(id, cancellationToken);
        if (model is null) return NotFound();
        return View(model);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken = default)
    {
        var deleted = await _productionPlanService.DeleteAsync(id, cancellationToken);
        if (!deleted) return NotFound();

        TempData["SuccessMessage"] = "Production plan deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<ProductOptionViewModel>> GetProductOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new ProductOptionViewModel { Id = x.Id, Name = x.Name, SKU = x.SKU })
            .ToListAsync(cancellationToken);
    }
}

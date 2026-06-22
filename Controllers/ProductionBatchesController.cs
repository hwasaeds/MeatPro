using MeatPro.Data;
using MeatPro.Models;
using MeatPro.Services;
using MeatPro.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MeatPro.Controllers;

[Authorize]
public sealed class ProductionBatchesController : Controller
{
    private readonly IProductionBatchService _productionBatchService;
    private readonly ApplicationDbContext _context;

    public ProductionBatchesController(IProductionBatchService productionBatchService, ApplicationDbContext context)
    {
        _productionBatchService = productionBatchService;
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, string? sort, int page = 1, CancellationToken cancellationToken = default)
    {
        var model = await _productionBatchService.BuildIndexAsync(search, sort, page, 10, cancellationToken);
        return View(model);
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
    {
        var model = await _productionBatchService.GetDetailsAsync(id, cancellationToken);
        if (model is null) return NotFound();
        return View(model);
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
    {
        var (workOrders, products) = await GetOptionsAsync(cancellationToken);
        return View(new ProductionBatchFormViewModel
        {
            ProducedAt = DateTime.UtcNow,
            WorkOrders = workOrders,
            Products = products
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductionBatchFormViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            var (workOrders, products) = await GetOptionsAsync(cancellationToken);
            model.WorkOrders = workOrders;
            model.Products = products;
            return View(model);
        }

        if (await _productionBatchService.IsBatchNumberInUseAsync(model.BatchNumber, null, cancellationToken))
        {
            ModelState.AddModelError(nameof(ProductionBatchFormViewModel.BatchNumber), "That batch number is already in use.");
            var (workOrders, products) = await GetOptionsAsync(cancellationToken);
            model.WorkOrders = workOrders;
            model.Products = products;
            return View(model);
        }

        try
        {
            var id = await _productionBatchService.CreateAsync(model, cancellationToken);
            TempData["SuccessMessage"] = $"Batch {model.BatchNumber} was created successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "The batch could not be saved. Please check the values and try again.");
            var (workOrders, products) = await GetOptionsAsync(cancellationToken);
            model.WorkOrders = workOrders;
            model.Products = products;
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
    {
        var model = await _productionBatchService.GetEditAsync(id, cancellationToken);
        if (model is null) return NotFound();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductionBatchFormViewModel model, CancellationToken cancellationToken = default)
    {
        if (id != model.Id) return NotFound();

        if (!ModelState.IsValid)
        {
            var (workOrders, products) = await GetOptionsAsync(cancellationToken);
            model.WorkOrders = workOrders;
            model.Products = products;
            return View(model);
        }

        if (await _productionBatchService.IsBatchNumberInUseAsync(model.BatchNumber, id, cancellationToken))
        {
            ModelState.AddModelError(nameof(ProductionBatchFormViewModel.BatchNumber), "That batch number is already in use.");
            var (workOrders, products) = await GetOptionsAsync(cancellationToken);
            model.WorkOrders = workOrders;
            model.Products = products;
            return View(model);
        }

        try
        {
            var updated = await _productionBatchService.UpdateAsync(id, model, cancellationToken);
            if (!updated) return NotFound();

            TempData["SuccessMessage"] = $"Batch {model.BatchNumber} was updated successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "The batch could not be updated. Please check the values and try again.");
            var (workOrders, products) = await GetOptionsAsync(cancellationToken);
            model.WorkOrders = workOrders;
            model.Products = products;
            return View(model);
        }
    }

    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var model = await _productionBatchService.GetDeleteAsync(id, cancellationToken);
        if (model is null) return NotFound();
        return View(model);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken = default)
    {
        var deleted = await _productionBatchService.DeleteAsync(id, cancellationToken);
        if (!deleted) return NotFound();

        TempData["SuccessMessage"] = "Production batch deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<(List<WorkOrderOptionViewModel>, List<ProductOptionViewModel>)> GetOptionsAsync(CancellationToken cancellationToken = default)
    {
        var workOrders = await _context.WorkOrders.AsNoTracking()
            .Include(x => x.ProductionPlan).ThenInclude(x => x!.Product)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new WorkOrderOptionViewModel { Id = x.Id, WorkOrderNumber = x.WorkOrderNumber, ProductName = x.ProductionPlan!.Product!.Name })
            .ToListAsync(cancellationToken);

        var products = await _context.Products.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new ProductOptionViewModel { Id = x.Id, Name = x.Name, SKU = x.SKU })
            .ToListAsync(cancellationToken);

        return (workOrders, products);
    }
}

using MeatPro.Data;
using MeatPro.Services;
using MeatPro.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MeatPro.Controllers;

[Authorize]
public sealed class ProductsController : Controller
{
    private readonly IProductService _productService;
    private readonly ApplicationDbContext _context;

    public ProductsController(IProductService productService, ApplicationDbContext context)
    {
        _productService = productService;
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, string? sort, int page = 1, CancellationToken cancellationToken = default)
    {
        var model = await _productService.BuildIndexAsync(search, sort, page, 10, cancellationToken);
        return View(model);
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
    {
        var model = await _productService.GetDetailsAsync(id, cancellationToken);
        if (model is null) return NotFound();
        return View(model);
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
    {
        var categories = await GetCategoryOptionsAsync(cancellationToken);
        return View(new ProductFormViewModel
        {
            UnitOfMeasure = "kg",
            IsActive = true,
            Categories = categories
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductFormViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            model.Categories = await GetCategoryOptionsAsync(cancellationToken);
            return View(model);
        }

        if (await _productService.IsSkuInUseAsync(model.SKU, null, cancellationToken))
        {
            ModelState.AddModelError(nameof(ProductFormViewModel.SKU), "That SKU is already in use.");
            model.Categories = await GetCategoryOptionsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var id = await _productService.CreateAsync(model, cancellationToken);
            TempData["SuccessMessage"] = $"Product {model.Name} was created successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "The product could not be saved. Please check the values and try again.");
            model.Categories = await GetCategoryOptionsAsync(cancellationToken);
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
    {
        var model = await _productService.GetEditAsync(id, cancellationToken);
        if (model is null) return NotFound();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductFormViewModel model, CancellationToken cancellationToken = default)
    {
        if (id != model.Id) return NotFound();

        if (!ModelState.IsValid)
        {
            model.Categories = await GetCategoryOptionsAsync(cancellationToken);
            return View(model);
        }

        if (await _productService.IsSkuInUseAsync(model.SKU, id, cancellationToken))
        {
            ModelState.AddModelError(nameof(ProductFormViewModel.SKU), "That SKU is already in use.");
            model.Categories = await GetCategoryOptionsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var updated = await _productService.UpdateAsync(id, model, cancellationToken);
            if (!updated) return NotFound();

            TempData["SuccessMessage"] = $"Product {model.Name} was updated successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "The product could not be updated. Please check the values and try again.");
            model.Categories = await GetCategoryOptionsAsync(cancellationToken);
            return View(model);
        }
    }

    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var model = await _productService.GetDeleteAsync(id, cancellationToken);
        if (model is null) return NotFound();
        return View(model);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken = default)
    {
        var deleted = await _productService.DeleteAsync(id, cancellationToken);
        if (!deleted) return NotFound();

        TempData["SuccessMessage"] = "Product deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<ProductCategoryOptionViewModel>> GetCategoryOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ProductCategories.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new ProductCategoryOptionViewModel { Id = x.Id, Name = x.Name })
            .ToListAsync(cancellationToken);
    }
}

using MeatPro.Data;
using MeatPro.Models;
using MeatPro.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace MeatPro.Services;

public interface IProductService
{
    Task<ProductIndexViewModel> BuildIndexAsync(string? search, string? sort, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ProductDetailsViewModel?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<ProductFormViewModel?> GetEditAsync(int id, CancellationToken cancellationToken = default);
    Task<ProductDetailsViewModel?> GetDeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> IsSkuInUseAsync(string sku, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(ProductFormViewModel model, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, ProductFormViewModel model, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class ProductService : IProductService
{
    private const int DefaultPageSize = 10;
    private readonly ApplicationDbContext _context;
    private readonly IAuditTrailService _auditTrail;

    public ProductService(ApplicationDbContext context, IAuditTrailService auditTrail)
    {
        _context = context;
        _auditTrail = auditTrail;
    }

    public async Task<ProductIndexViewModel> BuildIndexAsync(string? search, string? sort, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        pageSize = pageSize <= 0 ? DefaultPageSize : pageSize;
        var normalizedSearch = search?.Trim() ?? string.Empty;
        var normalizedSort = NormalizeSort(sort);

        IQueryable<Product> query = _context.Products.AsNoTracking().Include(x => x.ProductCategory);

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(x =>
                EF.Functions.Like(x.Name, $"%{normalizedSearch}%") ||
                EF.Functions.Like(x.SKU, $"%{normalizedSearch}%") ||
                EF.Functions.Like(x.ProductCategory!.Name, $"%{normalizedSearch}%"));
        }

        query = normalizedSort switch
        {
            "name" => query.OrderBy(x => x.Name).ThenBy(x => x.SKU),
            "name_desc" => query.OrderByDescending(x => x.Name).ThenByDescending(x => x.SKU),
            "price" => query.OrderBy(x => x.SellingPrice).ThenBy(x => x.Name),
            "price_desc" => query.OrderByDescending(x => x.SellingPrice).ThenBy(x => x.Name),
            _ => query.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Name)
        };

        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        var pageEntities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var pageItems = pageEntities.Select(x => new ProductListItemViewModel
        {
            Id = x.Id,
            Name = x.Name,
            SKU = x.SKU,
            CategoryName = x.ProductCategory?.Name ?? "Uncategorized",
            UnitOfMeasure = x.UnitOfMeasure,
            SellingPrice = x.SellingPrice,
            IsActive = x.IsActive,
            Status = x.IsActive ? "Active" : "Inactive",
            StatusTone = x.IsActive ? "success" : "secondary"
        }).ToList();

        var activeCount = await query.CountAsync(x => x.IsActive, cancellationToken);

        return new ProductIndexViewModel
        {
            Search = normalizedSearch,
            Sort = normalizedSort,
            Items = pageItems,
            TotalItems = totalItems,
            PageNumber = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            ActiveCount = activeCount
        };
    }

    public async Task<ProductDetailsViewModel?> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Products.AsNoTracking().Include(x => x.ProductCategory).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : MapDetails(entity);
    }

    public async Task<ProductFormViewModel?> GetEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Products.AsNoTracking().Include(x => x.ProductCategory).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return null;

        var model = MapForm(entity);
        model.Categories = await GetCategoryOptionsAsync(cancellationToken);
        return model;
    }

    public async Task<ProductDetailsViewModel?> GetDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await GetDetailsAsync(id, cancellationToken);
    }

    public async Task<bool> IsSkuInUseAsync(string sku, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedSku = sku.Trim();
        return await _context.Products.AsNoTracking().AnyAsync(x => x.SKU == normalizedSku && x.Id != (excludeId ?? 0), cancellationToken);
    }

    public async Task<int> CreateAsync(ProductFormViewModel model, CancellationToken cancellationToken = default)
    {
        var entity = new Product();
        ApplyForm(entity, model);
        _context.Products.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditTrail.LogAsync("Products", "Create", "Product", entity.Id.ToString(), null,
            null, new { entity.Name, entity.SKU }, cancellationToken: cancellationToken);

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(int id, ProductFormViewModel model, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Products.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return false;

        var oldValues = new { entity.Name, entity.SellingPrice, entity.IsActive };
        ApplyForm(entity, model);
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        await _auditTrail.LogAsync("Products", "Update", "Product", id.ToString(), null,
            oldValues, new { entity.Name, entity.SellingPrice, entity.IsActive }, cancellationToken: cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Products.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return false;

        var oldValues = new { entity.Name, entity.SKU };
        _context.Products.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditTrail.LogAsync("Products", "Delete", "Product", id.ToString(), null,
            oldValues, null, cancellationToken: cancellationToken);

        return true;
    }

    private async Task<List<ProductCategoryOptionViewModel>> GetCategoryOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ProductCategories.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new ProductCategoryOptionViewModel { Id = x.Id, Name = x.Name })
            .ToListAsync(cancellationToken);
    }

    private static string NormalizeSort(string? sort)
    {
        return sort?.Trim().ToLowerInvariant() switch
        {
            "name" => "name",
            "name_desc" => "name_desc",
            "price" => "price",
            "price_desc" => "price_desc",
            _ => "newest"
        };
    }

    private static ProductFormViewModel MapForm(Product entity)
    {
        return new ProductFormViewModel
        {
            Id = entity.Id,
            Name = entity.Name,
            SKU = entity.SKU,
            ProductCategoryId = entity.ProductCategoryId,
            Description = entity.Description,
            UnitOfMeasure = entity.UnitOfMeasure,
            StandardYield = entity.StandardYield,
            ReorderLevel = entity.ReorderLevel,
            SellingPrice = entity.SellingPrice,
            IsActive = entity.IsActive
        };
    }

    private static ProductDetailsViewModel MapDetails(Product entity)
    {
        return new ProductDetailsViewModel
        {
            Id = entity.Id,
            Name = entity.Name,
            SKU = entity.SKU,
            CategoryName = entity.ProductCategory?.Name ?? "Uncategorized",
            Description = entity.Description,
            UnitOfMeasure = entity.UnitOfMeasure,
            StandardYield = entity.StandardYield,
            ReorderLevel = entity.ReorderLevel,
            SellingPrice = entity.SellingPrice,
            IsActive = entity.IsActive,
            Status = entity.IsActive ? "Active" : "Inactive",
            StatusTone = entity.IsActive ? "success" : "secondary",
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc
        };
    }

    private static void ApplyForm(Product entity, ProductFormViewModel model)
    {
        entity.Name = model.Name.Trim();
        entity.SKU = model.SKU.Trim();
        entity.ProductCategoryId = model.ProductCategoryId;
        entity.Description = model.Description.Trim();
        entity.UnitOfMeasure = model.UnitOfMeasure.Trim();
        entity.StandardYield = model.StandardYield;
        entity.ReorderLevel = model.ReorderLevel;
        entity.SellingPrice = model.SellingPrice;
        entity.IsActive = model.IsActive;
    }
}

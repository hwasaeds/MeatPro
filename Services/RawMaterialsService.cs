using MeatPro.Data;
using MeatPro.Models;
using MeatPro.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace MeatPro.Services;

public interface IRawMaterialService
{
    Task<IReadOnlyList<RawMaterial>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<RawMaterialIndexViewModel> BuildIndexAsync(string? search, string? sort, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<RawMaterialDetailsViewModel?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<RawMaterialFormViewModel?> GetEditAsync(int id, CancellationToken cancellationToken = default);
    Task<RawMaterialDetailsViewModel?> GetDeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> IsSkuInUseAsync(string sku, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(RawMaterialFormViewModel model, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, RawMaterialFormViewModel model, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class RawMaterialService : IRawMaterialService
{
    private const int DefaultPageSize = 10;
    private readonly ApplicationDbContext _context;
    private readonly IAuditTrailService _auditTrail;

    public RawMaterialService(ApplicationDbContext context, IAuditTrailService auditTrail)
    {
        _context = context;
        _auditTrail = auditTrail;
    }

    public async Task<IReadOnlyList<RawMaterial>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.RawMaterials.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public async Task<RawMaterialIndexViewModel> BuildIndexAsync(string? search, string? sort, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        pageSize = pageSize <= 0 ? DefaultPageSize : pageSize;
        var normalizedSearch = search?.Trim() ?? string.Empty;
        var normalizedSort = NormalizeSort(sort);
        var expirationWarningCutoff = DateTime.UtcNow.AddDays(14);

        IQueryable<RawMaterial> query = _context.RawMaterials.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(x =>
                EF.Functions.Like(x.Name, $"%{normalizedSearch}%") ||
                EF.Functions.Like(x.SKU, $"%{normalizedSearch}%") ||
                EF.Functions.Like(x.Location, $"%{normalizedSearch}%") ||
                EF.Functions.Like(x.SupplierName, $"%{normalizedSearch}%"));
        }

        query = normalizedSort switch
        {
            "name" => query.OrderBy(x => x.Name).ThenBy(x => x.SKU),
            "name_desc" => query.OrderByDescending(x => x.Name).ThenByDescending(x => x.SKU),
            "stock" => query.OrderBy(x => x.CurrentStock).ThenBy(x => x.Name),
            "stock_desc" => query.OrderByDescending(x => x.CurrentStock).ThenBy(x => x.Name),
            "reorder" => query.OrderBy(x => x.ReorderLevel).ThenBy(x => x.Name),
            "reorder_desc" => query.OrderByDescending(x => x.ReorderLevel).ThenBy(x => x.Name),
            _ => query.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Name)
        };

        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        var pageEntities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var pageItems = pageEntities.Select(x => new RawMaterialListItemViewModel
        {
            Id = x.Id,
            SKU = x.SKU,
            Name = x.Name,
            UnitOfMeasure = x.UnitOfMeasure,
            CurrentStock = x.CurrentStock,
            ReorderLevel = x.ReorderLevel,
            Location = x.Location,
            SupplierName = x.SupplierName,
            Status = x.IsActive ? (x.CurrentStock <= x.ReorderLevel ? "Low stock" : "Healthy") : "Inactive",
            StatusTone = x.IsActive ? (x.CurrentStock <= x.ReorderLevel ? "warning" : "success") : "secondary",
            ExpirationLabel = x.ExpirationDate is null ? "No expiry" : x.ExpirationDate <= expirationWarningCutoff ? "Expiring soon" : x.ExpirationDate.Value.ToString("yyyy-MM-dd")
        }).ToList();

        var totalStock = await query.Select(x => x.CurrentStock).SumAsync(cancellationToken);
        var stockValue = await query.Select(x => x.CurrentStock * x.UnitCost).SumAsync(cancellationToken);
        var lowStockCount = await query.CountAsync(x => x.IsActive && x.CurrentStock <= x.ReorderLevel, cancellationToken);

        return new RawMaterialIndexViewModel
        {
            Search = normalizedSearch,
            Sort = normalizedSort,
            Items = pageItems,
            TotalItems = totalItems,
            PageNumber = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            TotalStock = totalStock,
            StockValue = stockValue,
            LowStockCount = lowStockCount
        };
    }

    public async Task<RawMaterialDetailsViewModel?> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.RawMaterials.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : MapDetails(entity);
    }

    public async Task<RawMaterialFormViewModel?> GetEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.RawMaterials.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : MapForm(entity);
    }

    public async Task<RawMaterialDetailsViewModel?> GetDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await GetDetailsAsync(id, cancellationToken);
    }

    public async Task<bool> IsSkuInUseAsync(string sku, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedSku = sku.Trim();
        return await _context.RawMaterials.AsNoTracking().AnyAsync(x => x.SKU == normalizedSku && x.Id != (excludeId ?? 0), cancellationToken);
    }

    public async Task<int> CreateAsync(RawMaterialFormViewModel model, CancellationToken cancellationToken = default)
    {
        var entity = new RawMaterial();
        ApplyForm(entity, model);
        _context.RawMaterials.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditTrail.LogAsync("Inventory", "Create", "RawMaterial", entity.Id.ToString(), null,
            null, new { entity.Name, entity.SKU, entity.CurrentStock }, cancellationToken: cancellationToken);

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(int id, RawMaterialFormViewModel model, CancellationToken cancellationToken = default)
    {
        var entity = await _context.RawMaterials.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        var oldValues = new { entity.Name, entity.SKU, entity.CurrentStock, entity.UnitCost };
        ApplyForm(entity, model);
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        await _auditTrail.LogAsync("Inventory", "Update", "RawMaterial", id.ToString(), null,
            oldValues, new { entity.Name, entity.SKU, entity.CurrentStock, entity.UnitCost }, cancellationToken: cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.RawMaterials.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        var oldValues = new { entity.Name, entity.SKU };
        _context.RawMaterials.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditTrail.LogAsync("Inventory", "Delete", "RawMaterial", id.ToString(), null,
            oldValues, null, cancellationToken: cancellationToken);

        return true;
    }

    private static string NormalizeSort(string? sort)
    {
        return sort?.Trim().ToLowerInvariant() switch
        {
            "name" => "name",
            "name_desc" => "name_desc",
            "stock" => "stock",
            "stock_desc" => "stock_desc",
            "reorder" => "reorder",
            "reorder_desc" => "reorder_desc",
            _ => "newest"
        };
    }

    private static RawMaterialFormViewModel MapForm(RawMaterial entity)
    {
        return new RawMaterialFormViewModel
        {
            Id = entity.Id,
            Name = entity.Name,
            SKU = entity.SKU,
            UnitOfMeasure = entity.UnitOfMeasure,
            CurrentStock = entity.CurrentStock,
            ReorderLevel = entity.ReorderLevel,
            UnitCost = entity.UnitCost,
            Location = entity.Location,
            ExpirationDate = entity.ExpirationDate,
            SupplierName = entity.SupplierName,
            IsActive = entity.IsActive
        };
    }

    private static RawMaterialDetailsViewModel MapDetails(RawMaterial entity)
    {
        var isLowStock = entity.IsActive && entity.CurrentStock <= entity.ReorderLevel;
        return new RawMaterialDetailsViewModel
        {
            Id = entity.Id,
            Name = entity.Name,
            SKU = entity.SKU,
            UnitOfMeasure = entity.UnitOfMeasure,
            CurrentStock = entity.CurrentStock,
            ReorderLevel = entity.ReorderLevel,
            UnitCost = entity.UnitCost,
            Location = entity.Location,
            ExpirationDate = entity.ExpirationDate,
            SupplierName = entity.SupplierName,
            IsActive = entity.IsActive,
            Status = entity.IsActive ? (isLowStock ? "Low stock" : "Healthy") : "Inactive",
            StatusTone = entity.IsActive ? (isLowStock ? "warning" : "success") : "secondary",
            StockValue = entity.CurrentStock * entity.UnitCost,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc
        };
    }

    private static void ApplyForm(RawMaterial entity, RawMaterialFormViewModel model)
    {
        entity.Name = model.Name.Trim();
        entity.SKU = model.SKU.Trim();
        entity.UnitOfMeasure = model.UnitOfMeasure.Trim();
        entity.CurrentStock = model.CurrentStock;
        entity.ReorderLevel = model.ReorderLevel;
        entity.UnitCost = model.UnitCost;
        entity.Location = model.Location.Trim();
        entity.ExpirationDate = model.ExpirationDate;
        entity.SupplierName = model.SupplierName.Trim();
        entity.IsActive = model.IsActive;
    }
}

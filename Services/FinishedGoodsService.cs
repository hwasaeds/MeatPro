using MeatPro.Data;
using MeatPro.Models;
using MeatPro.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace MeatPro.Services;

public interface IFinishedGoodService
{
    Task<FinishedGoodIndexViewModel> BuildIndexAsync(string? search, string? sort, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<FinishedGoodDetailsViewModel?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<FinishedGoodFormViewModel?> GetEditAsync(int id, CancellationToken cancellationToken = default);
    Task<FinishedGoodDetailsViewModel?> GetDeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> IsSkuInUseAsync(string sku, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(FinishedGoodFormViewModel model, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, FinishedGoodFormViewModel model, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class FinishedGoodService : IFinishedGoodService
{
    private const int DefaultPageSize = 10;
    private readonly ApplicationDbContext _context;
    private readonly IAuditTrailService _auditTrail;

    public FinishedGoodService(ApplicationDbContext context, IAuditTrailService auditTrail)
    {
        _context = context;
        _auditTrail = auditTrail;
    }

    public async Task<FinishedGoodIndexViewModel> BuildIndexAsync(string? search, string? sort, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        pageSize = pageSize <= 0 ? DefaultPageSize : pageSize;
        var normalizedSearch = search?.Trim() ?? string.Empty;
        var normalizedSort = NormalizeSort(sort);
        var expiryCutoff = DateTime.UtcNow.AddDays(14);

        IQueryable<FinishedGood> query = _context.FinishedGoods.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(x =>
                EF.Functions.Like(x.Name, $"%{normalizedSearch}%") ||
                EF.Functions.Like(x.SKU, $"%{normalizedSearch}%") ||
                EF.Functions.Like(x.BatchNumber, $"%{normalizedSearch}%") ||
                EF.Functions.Like(x.StorageLocation, $"%{normalizedSearch}%"));
        }

        query = normalizedSort switch
        {
            "name" => query.OrderBy(x => x.Name).ThenBy(x => x.SKU),
            "name_desc" => query.OrderByDescending(x => x.Name).ThenByDescending(x => x.SKU),
            "stock" => query.OrderBy(x => x.CurrentStock).ThenBy(x => x.Name),
            "stock_desc" => query.OrderByDescending(x => x.CurrentStock).ThenBy(x => x.Name),
            "price" => query.OrderBy(x => x.UnitPrice).ThenBy(x => x.Name),
            "price_desc" => query.OrderByDescending(x => x.UnitPrice).ThenBy(x => x.Name),
            _ => query.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Name)
        };

        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        var pageEntities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var pageItems = pageEntities.Select(x =>
        {
            var isLowStock = x.IsActive && x.CurrentStock <= x.ReorderLevel;
            var isExpiring = x.ExpirationDate is not null && x.ExpirationDate <= expiryCutoff;
            return new FinishedGoodListItemViewModel
            {
                Id = x.Id,
                Name = x.Name,
                SKU = x.SKU,
                BatchNumber = x.BatchNumber,
                CurrentStock = x.CurrentStock,
                ReorderLevel = x.ReorderLevel,
                UnitPrice = x.UnitPrice,
                StorageLocation = x.StorageLocation,
                ExpirationDate = x.ExpirationDate,
                Status = !x.IsActive ? "Inactive" : isExpiring ? "Expiring" : isLowStock ? "Low stock" : "In stock",
                StatusTone = !x.IsActive ? "secondary" : isExpiring ? "warning" : isLowStock ? "warning" : "success"
            };
        }).ToList();

        var totalUnits = await query.SumAsync(x => x.CurrentStock, cancellationToken);
        var totalValue = await query.SumAsync(x => x.CurrentStock * x.UnitPrice, cancellationToken);
        var expiringCount = await query.CountAsync(x => x.IsActive && x.ExpirationDate != null && x.ExpirationDate <= expiryCutoff, cancellationToken);

        return new FinishedGoodIndexViewModel
        {
            Search = normalizedSearch,
            Sort = normalizedSort,
            Items = pageItems,
            TotalItems = totalItems,
            PageNumber = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            TotalUnits = totalUnits,
            TotalValue = totalValue,
            ExpiringCount = expiringCount
        };
    }

    public async Task<FinishedGoodDetailsViewModel?> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.FinishedGoods.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : MapDetails(entity);
    }

    public async Task<FinishedGoodFormViewModel?> GetEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.FinishedGoods.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : MapForm(entity);
    }

    public async Task<FinishedGoodDetailsViewModel?> GetDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await GetDetailsAsync(id, cancellationToken);
    }

    public async Task<bool> IsSkuInUseAsync(string sku, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedSku = sku.Trim();
        return await _context.FinishedGoods.AsNoTracking().AnyAsync(x => x.SKU == normalizedSku && x.Id != (excludeId ?? 0), cancellationToken);
    }

    public async Task<int> CreateAsync(FinishedGoodFormViewModel model, CancellationToken cancellationToken = default)
    {
        var entity = new FinishedGood();
        ApplyForm(entity, model);
        _context.FinishedGoods.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditTrail.LogAsync("Inventory", "Create", "FinishedGood", entity.Id.ToString(), null,
            null, new { entity.Name, entity.SKU, entity.CurrentStock }, cancellationToken: cancellationToken);

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(int id, FinishedGoodFormViewModel model, CancellationToken cancellationToken = default)
    {
        var entity = await _context.FinishedGoods.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return false;

        var oldValues = new { entity.Name, entity.CurrentStock, entity.UnitPrice };
        ApplyForm(entity, model);
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        await _auditTrail.LogAsync("Inventory", "Update", "FinishedGood", id.ToString(), null,
            oldValues, new { entity.Name, entity.CurrentStock, entity.UnitPrice }, cancellationToken: cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.FinishedGoods.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return false;

        var oldValues = new { entity.Name, entity.SKU };
        _context.FinishedGoods.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditTrail.LogAsync("Inventory", "Delete", "FinishedGood", id.ToString(), null,
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
            "price" => "price",
            "price_desc" => "price_desc",
            _ => "newest"
        };
    }

    private static FinishedGoodFormViewModel MapForm(FinishedGood entity)
    {
        return new FinishedGoodFormViewModel
        {
            Id = entity.Id,
            Name = entity.Name,
            SKU = entity.SKU,
            BatchNumber = entity.BatchNumber,
            CurrentStock = entity.CurrentStock,
            ReorderLevel = entity.ReorderLevel,
            UnitPrice = entity.UnitPrice,
            StorageLocation = entity.StorageLocation,
            ExpirationDate = entity.ExpirationDate,
            IsActive = entity.IsActive
        };
    }

    private static FinishedGoodDetailsViewModel MapDetails(FinishedGood entity)
    {
        var isLowStock = entity.IsActive && entity.CurrentStock <= entity.ReorderLevel;
        var isExpiring = entity.ExpirationDate is not null && entity.ExpirationDate <= DateTime.UtcNow.AddDays(14);
        return new FinishedGoodDetailsViewModel
        {
            Id = entity.Id,
            Name = entity.Name,
            SKU = entity.SKU,
            BatchNumber = entity.BatchNumber,
            CurrentStock = entity.CurrentStock,
            ReorderLevel = entity.ReorderLevel,
            UnitPrice = entity.UnitPrice,
            StorageLocation = entity.StorageLocation,
            ExpirationDate = entity.ExpirationDate,
            IsActive = entity.IsActive,
            Status = !entity.IsActive ? "Inactive" : isExpiring ? "Expiring" : isLowStock ? "Low stock" : "In stock",
            StatusTone = !entity.IsActive ? "secondary" : isExpiring ? "warning" : isLowStock ? "warning" : "success",
            StockValue = entity.CurrentStock * entity.UnitPrice,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc
        };
    }

    private static void ApplyForm(FinishedGood entity, FinishedGoodFormViewModel model)
    {
        entity.Name = model.Name.Trim();
        entity.SKU = model.SKU.Trim();
        entity.BatchNumber = model.BatchNumber.Trim();
        entity.CurrentStock = model.CurrentStock;
        entity.ReorderLevel = model.ReorderLevel;
        entity.UnitPrice = model.UnitPrice;
        entity.StorageLocation = model.StorageLocation.Trim();
        entity.ExpirationDate = model.ExpirationDate;
        entity.IsActive = model.IsActive;
    }
}

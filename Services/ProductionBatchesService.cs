using MeatPro.Data;
using MeatPro.Models;
using MeatPro.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace MeatPro.Services;

public interface IProductionBatchService
{
    Task<ProductionBatchIndexViewModel> BuildIndexAsync(string? search, string? sort, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ProductionBatchDetailsViewModel?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<ProductionBatchFormViewModel?> GetEditAsync(int id, CancellationToken cancellationToken = default);
    Task<ProductionBatchDetailsViewModel?> GetDeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> IsBatchNumberInUseAsync(string batchNumber, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(ProductionBatchFormViewModel model, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, ProductionBatchFormViewModel model, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class ProductionBatchService : IProductionBatchService
{
    private const int DefaultPageSize = 10;
    private readonly ApplicationDbContext _context;
    private readonly IAuditTrailService _auditTrail;
    private readonly IFinishedGoodService _finishedGoodService;
    private readonly IStockService _stockService;

    public ProductionBatchService(ApplicationDbContext context, IAuditTrailService auditTrail, IFinishedGoodService finishedGoodService, IStockService stockService)
    {
        _context = context;
        _auditTrail = auditTrail;
        _finishedGoodService = finishedGoodService;
        _stockService = stockService;
    }

    public async Task<ProductionBatchIndexViewModel> BuildIndexAsync(string? search, string? sort, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        pageSize = pageSize <= 0 ? DefaultPageSize : pageSize;
        var normalizedSearch = search?.Trim() ?? string.Empty;
        var normalizedSort = NormalizeSort(sort);
        var expiryCutoff = DateTime.UtcNow.AddDays(14);

        IQueryable<ProductionBatch> query = _context.ProductionBatches.AsNoTracking()
            .Include(x => x.Product)
            .Include(x => x.WorkOrder);

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(x =>
                EF.Functions.Like(x.BatchNumber, $"%{normalizedSearch}%") ||
                EF.Functions.Like(x.TraceabilityCode, $"%{normalizedSearch}%") ||
                EF.Functions.Like(x.Product!.Name, $"%{normalizedSearch}%") ||
                EF.Functions.Like(x.WorkOrder!.WorkOrderNumber, $"%{normalizedSearch}%"));
        }

        query = normalizedSort switch
        {
            "batch" => query.OrderBy(x => x.BatchNumber),
            "batch_desc" => query.OrderByDescending(x => x.BatchNumber),
            "produced" => query.OrderBy(x => x.ProducedAt),
            "produced_desc" => query.OrderByDescending(x => x.ProducedAt),
            "quantity" => query.OrderBy(x => x.ProducedQuantity),
            "quantity_desc" => query.OrderByDescending(x => x.ProducedQuantity),
            _ => query.OrderByDescending(x => x.ProducedAt)
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
            var isExpiring = x.ExpirationDate is not null && x.ExpirationDate <= expiryCutoff;
            return new ProductionBatchListItemViewModel
            {
                Id = x.Id,
                BatchNumber = x.BatchNumber,
                WorkOrderNumber = x.WorkOrder?.WorkOrderNumber ?? "Unknown",
                ProductName = x.Product?.Name ?? "Unknown",
                TraceabilityCode = x.TraceabilityCode,
                ProducedQuantity = x.ProducedQuantity,
                ProducedAt = x.ProducedAt,
                ExpirationDate = x.ExpirationDate,
                Status = isExpiring ? "Expiring" : "Good",
                StatusTone = isExpiring ? "warning" : "success"
            };
        }).ToList();

        var activeBatchCount = await query.CountAsync(x => x.ExpirationDate == null || x.ExpirationDate > DateTime.UtcNow, cancellationToken);
        var expiringCount = await query.CountAsync(x => x.ExpirationDate != null && x.ExpirationDate <= expiryCutoff, cancellationToken);
        var totalProduced = await query.SumAsync(x => x.ProducedQuantity, cancellationToken);

        return new ProductionBatchIndexViewModel
        {
            Search = normalizedSearch,
            Sort = normalizedSort,
            Items = pageItems,
            TotalItems = totalItems,
            PageNumber = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            ActiveBatchCount = activeBatchCount,
            ExpiringCount = expiringCount,
            TotalProduced = totalProduced
        };
    }

    public async Task<ProductionBatchDetailsViewModel?> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ProductionBatches.AsNoTracking()
            .Include(x => x.Product)
            .Include(x => x.WorkOrder)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : MapDetails(entity);
    }

    public async Task<ProductionBatchFormViewModel?> GetEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ProductionBatches.AsNoTracking()
            .Include(x => x.Product)
            .Include(x => x.WorkOrder)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return null;

        var model = MapForm(entity);
        model.WorkOrders = await GetWorkOrderOptionsAsync(cancellationToken);
        model.Products = await GetProductOptionsAsync(cancellationToken);
        return model;
    }

    public async Task<ProductionBatchDetailsViewModel?> GetDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await GetDetailsAsync(id, cancellationToken);
    }

    public async Task<bool> IsBatchNumberInUseAsync(string batchNumber, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalized = batchNumber.Trim();
        return await _context.ProductionBatches.AsNoTracking().AnyAsync(x => x.BatchNumber == normalized && x.Id != (excludeId ?? 0), cancellationToken);
    }

    public async Task<int> CreateAsync(ProductionBatchFormViewModel model, CancellationToken cancellationToken = default)
    {
        var entity = new ProductionBatch();
        ApplyForm(entity, model);
        _context.ProductionBatches.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == model.ProductId, cancellationToken);
        var productName = product?.Name ?? string.Empty;
        var fg = !string.IsNullOrWhiteSpace(productName)
            ? await _context.FinishedGoods.FirstOrDefaultAsync(x => x.Name.Contains(productName), cancellationToken)
            : null;
        if (fg is not null && model.ProducedQuantity > 0)
        {
            var performedBy = "system";
            await _stockService.RecordProductionOutputAsync(fg.Id, model.ProducedQuantity, model.BatchNumber, performedBy, cancellationToken);
        }

        await _auditTrail.LogAsync("Production", "Create", "ProductionBatch", entity.Id.ToString(), null,
            null, new { entity.BatchNumber, entity.ProducedQuantity, entity.TraceabilityCode }, cancellationToken: cancellationToken);

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(int id, ProductionBatchFormViewModel model, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ProductionBatches.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return false;

        var oldValues = new { entity.BatchNumber, entity.ProducedQuantity, entity.RawMaterialConsumed };
        ApplyForm(entity, model);
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        await _auditTrail.LogAsync("Production", "Update", "ProductionBatch", id.ToString(), null,
            oldValues, new { entity.BatchNumber, entity.ProducedQuantity, entity.RawMaterialConsumed }, cancellationToken: cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ProductionBatches.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return false;

        var oldValues = new { entity.BatchNumber };
        _context.ProductionBatches.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditTrail.LogAsync("Production", "Delete", "ProductionBatch", id.ToString(), null,
            oldValues, null, cancellationToken: cancellationToken);

        return true;
    }

    private async Task<List<WorkOrderOptionViewModel>> GetWorkOrderOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.WorkOrders.AsNoTracking()
            .Include(x => x.ProductionPlan).ThenInclude(x => x!.Product)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new WorkOrderOptionViewModel { Id = x.Id, WorkOrderNumber = x.WorkOrderNumber, ProductName = x.ProductionPlan!.Product!.Name })
            .ToListAsync(cancellationToken);
    }

    private async Task<List<ProductOptionViewModel>> GetProductOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new ProductOptionViewModel { Id = x.Id, Name = x.Name, SKU = x.SKU })
            .ToListAsync(cancellationToken);
    }

    private static string NormalizeSort(string? sort)
    {
        return sort?.Trim().ToLowerInvariant() switch
        {
            "batch" => "batch",
            "batch_desc" => "batch_desc",
            "produced" => "produced",
            "produced_desc" => "produced_desc",
            "quantity" => "quantity",
            "quantity_desc" => "quantity_desc",
            _ => "newest"
        };
    }

    private static ProductionBatchFormViewModel MapForm(ProductionBatch entity)
    {
        return new ProductionBatchFormViewModel
        {
            Id = entity.Id,
            BatchNumber = entity.BatchNumber,
            WorkOrderId = entity.WorkOrderId,
            ProductId = entity.ProductId,
            TraceabilityCode = entity.TraceabilityCode,
            RawMaterialConsumed = entity.RawMaterialConsumed,
            ProducedQuantity = entity.ProducedQuantity,
            ProducedAt = entity.ProducedAt,
            ExpirationDate = entity.ExpirationDate,
            Notes = entity.Notes
        };
    }

    private static ProductionBatchDetailsViewModel MapDetails(ProductionBatch entity)
    {
        var isExpiring = entity.ExpirationDate is not null && entity.ExpirationDate <= DateTime.UtcNow.AddDays(14);
        return new ProductionBatchDetailsViewModel
        {
            Id = entity.Id,
            BatchNumber = entity.BatchNumber,
            WorkOrderId = entity.WorkOrderId,
            WorkOrderNumber = entity.WorkOrder?.WorkOrderNumber ?? "Unknown",
            ProductId = entity.ProductId,
            ProductName = entity.Product?.Name ?? "Unknown",
            TraceabilityCode = entity.TraceabilityCode,
            RawMaterialConsumed = entity.RawMaterialConsumed,
            ProducedQuantity = entity.ProducedQuantity,
            ProducedAt = entity.ProducedAt,
            ExpirationDate = entity.ExpirationDate,
            Notes = entity.Notes,
            Status = isExpiring ? "Expiring" : "Good",
            StatusTone = isExpiring ? "warning" : "success",
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc
        };
    }

    private static void ApplyForm(ProductionBatch entity, ProductionBatchFormViewModel model)
    {
        entity.BatchNumber = model.BatchNumber.Trim();
        entity.WorkOrderId = model.WorkOrderId;
        entity.ProductId = model.ProductId;
        entity.TraceabilityCode = model.TraceabilityCode.Trim();
        entity.RawMaterialConsumed = model.RawMaterialConsumed;
        entity.ProducedQuantity = model.ProducedQuantity;
        entity.ProducedAt = model.ProducedAt;
        entity.ExpirationDate = model.ExpirationDate;
        entity.Notes = model.Notes.Trim();
    }
}

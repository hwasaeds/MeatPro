using MeatPro.Data;
using MeatPro.Models;
using MeatPro.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace MeatPro.Services;

public interface IProductionPlanService
{
    Task<ProductionPlanIndexViewModel> BuildIndexAsync(string? search, string? sort, string? statusFilter, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ProductionPlanDetailsViewModel?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<ProductionPlanFormViewModel?> GetEditAsync(int id, CancellationToken cancellationToken = default);
    Task<ProductionPlanDetailsViewModel?> GetDeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> IsPlanCodeInUseAsync(string planCode, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(ProductionPlanFormViewModel model, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, ProductionPlanFormViewModel model, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class ProductionPlanService : IProductionPlanService
{
    private const int DefaultPageSize = 10;
    private readonly ApplicationDbContext _context;
    private readonly IAuditTrailService _auditTrail;

    public ProductionPlanService(ApplicationDbContext context, IAuditTrailService auditTrail)
    {
        _context = context;
        _auditTrail = auditTrail;
    }

    public async Task<ProductionPlanIndexViewModel> BuildIndexAsync(string? search, string? sort, string? statusFilter, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        pageSize = pageSize <= 0 ? DefaultPageSize : pageSize;
        var normalizedSearch = search?.Trim() ?? string.Empty;
        var normalizedSort = NormalizeSort(sort);

        IQueryable<ProductionPlan> query = _context.ProductionPlans.AsNoTracking().Include(x => x.Product);

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(x =>
                EF.Functions.Like(x.PlanCode, $"%{normalizedSearch}%") ||
                EF.Functions.Like(x.Product!.Name, $"%{normalizedSearch}%") ||
                EF.Functions.Like(x.Notes, $"%{normalizedSearch}%"));
        }

        if (!string.IsNullOrWhiteSpace(statusFilter) && Enum.TryParse<ProductionPlanStatus>(statusFilter, true, out var parsedStatus))
        {
            query = query.Where(x => x.Status == parsedStatus);
        }

        query = normalizedSort switch
        {
            "code" => query.OrderBy(x => x.PlanCode),
            "code_desc" => query.OrderByDescending(x => x.PlanCode),
            "start" => query.OrderBy(x => x.StartDate),
            "start_desc" => query.OrderByDescending(x => x.StartDate),
            "quantity" => query.OrderBy(x => x.PlannedQuantity),
            "quantity_desc" => query.OrderByDescending(x => x.PlannedQuantity),
            _ => query.OrderByDescending(x => x.CreatedAtUtc)
        };

        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        var pageEntities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var pageItems = pageEntities.Select(x => new ProductionPlanListItemViewModel
        {
            Id = x.Id,
            PlanCode = x.PlanCode,
            ProductName = x.Product?.Name ?? "Unknown",
            PlannedQuantity = x.PlannedQuantity,
            StartDate = x.StartDate,
            EndDate = x.EndDate,
            Status = x.Status.ToString(),
            StatusTone = x.Status switch
            {
                ProductionPlanStatus.Completed => "success",
                ProductionPlanStatus.InProgress => "primary",
                ProductionPlanStatus.Approved => "info",
                ProductionPlanStatus.Cancelled => "danger",
                _ => "secondary"
            }
        }).ToList();

        var activePlanCount = await query.CountAsync(x => x.Status == ProductionPlanStatus.Approved || x.Status == ProductionPlanStatus.InProgress, cancellationToken);
        var draftCount = await query.CountAsync(x => x.Status == ProductionPlanStatus.Draft, cancellationToken);
        var totalPlannedQuantity = await query.SumAsync(x => x.PlannedQuantity, cancellationToken);

        return new ProductionPlanIndexViewModel
        {
            Search = normalizedSearch,
            Sort = normalizedSort,
            StatusFilter = statusFilter,
            Items = pageItems,
            TotalItems = totalItems,
            PageNumber = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            ActivePlanCount = activePlanCount,
            DraftCount = draftCount,
            TotalPlannedQuantity = totalPlannedQuantity
        };
    }

    public async Task<ProductionPlanDetailsViewModel?> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ProductionPlans.AsNoTracking().Include(x => x.Product).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : MapDetails(entity);
    }

    public async Task<ProductionPlanFormViewModel?> GetEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ProductionPlans.AsNoTracking().Include(x => x.Product).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return null;

        var model = MapForm(entity);
        model.Products = await GetProductOptionsAsync(cancellationToken);
        return model;
    }

    public async Task<ProductionPlanDetailsViewModel?> GetDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await GetDetailsAsync(id, cancellationToken);
    }

    public async Task<bool> IsPlanCodeInUseAsync(string planCode, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalized = planCode.Trim();
        return await _context.ProductionPlans.AsNoTracking().AnyAsync(x => x.PlanCode == normalized && x.Id != (excludeId ?? 0), cancellationToken);
    }

    public async Task<int> CreateAsync(ProductionPlanFormViewModel model, CancellationToken cancellationToken = default)
    {
        var entity = new ProductionPlan();
        ApplyForm(entity, model);
        _context.ProductionPlans.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditTrail.LogAsync("Production", "Create", "ProductionPlan", entity.Id.ToString(), null,
            null, new { entity.PlanCode, entity.PlannedQuantity, entity.Status }, cancellationToken: cancellationToken);

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(int id, ProductionPlanFormViewModel model, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ProductionPlans.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return false;

        var oldValues = new { entity.PlanCode, entity.Status, entity.PlannedQuantity };
        ApplyForm(entity, model);
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        await _auditTrail.LogAsync("Production", "Update", "ProductionPlan", id.ToString(), null,
            oldValues, new { entity.PlanCode, entity.Status, entity.PlannedQuantity }, cancellationToken: cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ProductionPlans.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return false;

        var oldValues = new { entity.PlanCode };
        _context.ProductionPlans.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditTrail.LogAsync("Production", "Delete", "ProductionPlan", id.ToString(), null,
            oldValues, null, cancellationToken: cancellationToken);

        return true;
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
            "code" => "code",
            "code_desc" => "code_desc",
            "start" => "start",
            "start_desc" => "start_desc",
            "quantity" => "quantity",
            "quantity_desc" => "quantity_desc",
            _ => "newest"
        };
    }

    private static ProductionPlanFormViewModel MapForm(ProductionPlan entity)
    {
        return new ProductionPlanFormViewModel
        {
            Id = entity.Id,
            PlanCode = entity.PlanCode,
            ProductId = entity.ProductId,
            PlannedQuantity = entity.PlannedQuantity,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Status = entity.Status,
            Notes = entity.Notes,
            CreatedByUser = entity.CreatedByUser
        };
    }

    private static ProductionPlanDetailsViewModel MapDetails(ProductionPlan entity)
    {
        return new ProductionPlanDetailsViewModel
        {
            Id = entity.Id,
            PlanCode = entity.PlanCode,
            ProductId = entity.ProductId,
            ProductName = entity.Product?.Name ?? "Unknown",
            PlannedQuantity = entity.PlannedQuantity,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Status = entity.Status.ToString(),
            StatusTone = entity.Status switch
            {
                ProductionPlanStatus.Completed => "success",
                ProductionPlanStatus.InProgress => "primary",
                ProductionPlanStatus.Approved => "info",
                ProductionPlanStatus.Cancelled => "danger",
                _ => "secondary"
            },
            Notes = entity.Notes,
            CreatedByUser = entity.CreatedByUser,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc
        };
    }

    private static void ApplyForm(ProductionPlan entity, ProductionPlanFormViewModel model)
    {
        entity.PlanCode = model.PlanCode.Trim();
        entity.ProductId = model.ProductId;
        entity.PlannedQuantity = model.PlannedQuantity;
        entity.StartDate = model.StartDate;
        entity.EndDate = model.EndDate;
        entity.Status = model.Status;
        entity.Notes = model.Notes.Trim();
        entity.CreatedByUser = model.CreatedByUser.Trim();
    }
}

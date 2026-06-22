using MeatPro.Data;
using MeatPro.Models;
using MeatPro.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace MeatPro.Services;

public interface IWorkOrderService
{
    Task<WorkOrderIndexViewModel> BuildIndexAsync(string? search, string? sort, string? statusFilter, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<WorkOrderDetailsViewModel?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<WorkOrderFormViewModel?> GetEditAsync(int id, CancellationToken cancellationToken = default);
    Task<WorkOrderDetailsViewModel?> GetDeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> IsWorkOrderNumberInUseAsync(string workOrderNumber, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(WorkOrderFormViewModel model, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, WorkOrderFormViewModel model, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ApproveAsync(int id, string performedBy, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> StartAsync(int id, string performedBy, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> HoldAsync(int id, string performedBy, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> ResumeAsync(int id, string performedBy, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> CompleteAsync(int id, CompleteWorkOrderViewModel model, string performedBy, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> CancelAsync(int id, string performedBy, CancellationToken cancellationToken = default);
}

public sealed class WorkOrderService : IWorkOrderService
{
    private const int DefaultPageSize = 10;
    private readonly ApplicationDbContext _context;
    private readonly IAuditTrailService _auditTrail;

    public WorkOrderService(ApplicationDbContext context, IAuditTrailService auditTrail)
    {
        _context = context;
        _auditTrail = auditTrail;
    }

    public async Task<WorkOrderIndexViewModel> BuildIndexAsync(string? search, string? sort, string? statusFilter, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        pageSize = pageSize <= 0 ? DefaultPageSize : pageSize;
        var normalizedSearch = search?.Trim() ?? string.Empty;
        var normalizedSort = NormalizeSort(sort);

        IQueryable<WorkOrder> query = _context.WorkOrders.AsNoTracking()
            .Include(x => x.ProductionPlan).ThenInclude(x => x!.Product);

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(x =>
                EF.Functions.Like(x.WorkOrderNumber, $"%{normalizedSearch}%") ||
                EF.Functions.Like(x.AssignedTo, $"%{normalizedSearch}%") ||
                EF.Functions.Like(x.ProductionPlan!.PlanCode, $"%{normalizedSearch}%") ||
                EF.Functions.Like(x.Notes, $"%{normalizedSearch}%"));
        }

        if (!string.IsNullOrWhiteSpace(statusFilter) && Enum.TryParse<WorkOrderStatus>(statusFilter, true, out var parsedStatus))
        {
            query = query.Where(x => x.Status == parsedStatus);
        }

        query = normalizedSort switch
        {
            "number" => query.OrderBy(x => x.WorkOrderNumber),
            "number_desc" => query.OrderByDescending(x => x.WorkOrderNumber),
            "date" => query.OrderBy(x => x.ScheduledDate),
            "date_desc" => query.OrderByDescending(x => x.ScheduledDate),
            "quantity" => query.OrderBy(x => x.Quantity),
            "quantity_desc" => query.OrderByDescending(x => x.Quantity),
            _ => query.OrderByDescending(x => x.CreatedAtUtc)
        };

        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        var pageEntities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var pageItems = pageEntities.Select(x => new WorkOrderListItemViewModel
        {
            Id = x.Id,
            WorkOrderNumber = x.WorkOrderNumber,
            PlanCode = x.ProductionPlan?.PlanCode ?? "Unknown",
            ProductName = x.ProductionPlan?.Product?.Name ?? "Unknown",
            ScheduledDate = x.ScheduledDate,
            Quantity = x.Quantity,
            OutputQuantity = x.OutputQuantity,
            AssignedTo = x.AssignedTo,
            Status = x.Status.ToString(),
            StatusTone = x.Status switch
            {
                WorkOrderStatus.Completed => "success",
                WorkOrderStatus.InProgress => "primary",
                WorkOrderStatus.Planned => "info",
                WorkOrderStatus.OnHold => "warning",
                WorkOrderStatus.Cancelled => "danger",
                _ => "secondary"
            }
        }).ToList();

        var openCount = await query.CountAsync(x => x.Status == WorkOrderStatus.Draft || x.Status == WorkOrderStatus.Planned || x.Status == WorkOrderStatus.InProgress || x.Status == WorkOrderStatus.OnHold, cancellationToken);
        var completedCount = await query.CountAsync(x => x.Status == WorkOrderStatus.Completed, cancellationToken);

        return new WorkOrderIndexViewModel
        {
            Search = normalizedSearch,
            Sort = normalizedSort,
            StatusFilter = statusFilter,
            Items = pageItems,
            TotalItems = totalItems,
            PageNumber = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            OpenCount = openCount,
            CompletedCount = completedCount
        };
    }

    public async Task<WorkOrderDetailsViewModel?> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.WorkOrders.AsNoTracking()
            .Include(x => x.ProductionPlan).ThenInclude(x => x!.Product)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : MapDetails(entity);
    }

    public async Task<WorkOrderFormViewModel?> GetEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.WorkOrders.AsNoTracking()
            .Include(x => x.ProductionPlan).ThenInclude(x => x!.Product)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return null;

        var model = MapForm(entity);
        model.ProductionPlans = await GetProductionPlanOptionsAsync(cancellationToken);
        return model;
    }

    public async Task<WorkOrderDetailsViewModel?> GetDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await GetDetailsAsync(id, cancellationToken);
    }

    public async Task<bool> IsWorkOrderNumberInUseAsync(string workOrderNumber, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalized = workOrderNumber.Trim();
        return await _context.WorkOrders.AsNoTracking().AnyAsync(x => x.WorkOrderNumber == normalized && x.Id != (excludeId ?? 0), cancellationToken);
    }

    public async Task<int> CreateAsync(WorkOrderFormViewModel model, CancellationToken cancellationToken = default)
    {
        var entity = new WorkOrder();
        ApplyForm(entity, model);
        _context.WorkOrders.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditTrail.LogAsync("Production", "Create", "WorkOrder", entity.Id.ToString(), null,
            null, new { entity.WorkOrderNumber, entity.Quantity, entity.Status }, cancellationToken: cancellationToken);

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(int id, WorkOrderFormViewModel model, CancellationToken cancellationToken = default)
    {
        var entity = await _context.WorkOrders.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return false;

        var oldValues = new { entity.WorkOrderNumber, entity.Status, entity.Quantity, entity.OutputQuantity };
        ApplyForm(entity, model);
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        await _auditTrail.LogAsync("Production", "Update", "WorkOrder", id.ToString(), null,
            oldValues, new { entity.WorkOrderNumber, entity.Status, entity.Quantity, entity.OutputQuantity }, cancellationToken: cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.WorkOrders.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return false;

        var oldValues = new { entity.WorkOrderNumber };
        _context.WorkOrders.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditTrail.LogAsync("Production", "Delete", "WorkOrder", id.ToString(), null,
            oldValues, null, cancellationToken: cancellationToken);

        return true;
    }

    public async Task<(bool Success, string Message)> ApproveAsync(int id, string performedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _context.WorkOrders.Include(x => x.ProductionPlan).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return (false, "Work order not found.");
        if (entity.Status != WorkOrderStatus.Draft) return (false, $"Cannot approve a work order in '{entity.Status}' status.");

        var oldStatus = entity.Status;
        entity.Status = WorkOrderStatus.Planned;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        await _auditTrail.LogAsync("Production", "Approve", "WorkOrder", id.ToString(), performedBy,
            new { Status = oldStatus.ToString() }, new { Status = WorkOrderStatus.Planned.ToString() }, cancellationToken: cancellationToken);
        return (true, $"Work order {entity.WorkOrderNumber} approved and moved to Planned.");
    }

    public async Task<(bool Success, string Message)> StartAsync(int id, string performedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _context.WorkOrders.Include(x => x.ProductionPlan).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return (false, "Work order not found.");
        if (entity.Status != WorkOrderStatus.Planned) return (false, $"Cannot start a work order in '{entity.Status}' status.");

        var oldStatus = entity.Status;
        entity.Status = WorkOrderStatus.InProgress;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        await _auditTrail.LogAsync("Production", "Start", "WorkOrder", id.ToString(), performedBy,
            new { Status = oldStatus.ToString() }, new { Status = WorkOrderStatus.InProgress.ToString() }, cancellationToken: cancellationToken);
        return (true, $"Work order {entity.WorkOrderNumber} started.");
    }

    public async Task<(bool Success, string Message)> HoldAsync(int id, string performedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _context.WorkOrders.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return (false, "Work order not found.");
        if (entity.Status != WorkOrderStatus.InProgress) return (false, "Only in-progress work orders can be put on hold.");

        var oldStatus = entity.Status;
        entity.Status = WorkOrderStatus.OnHold;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        await _auditTrail.LogAsync("Production", "Hold", "WorkOrder", id.ToString(), performedBy,
            new { Status = oldStatus.ToString() }, new { Status = WorkOrderStatus.OnHold.ToString() }, cancellationToken: cancellationToken);
        return (true, $"Work order {entity.WorkOrderNumber} put on hold.");
    }

    public async Task<(bool Success, string Message)> ResumeAsync(int id, string performedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _context.WorkOrders.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return (false, "Work order not found.");
        if (entity.Status != WorkOrderStatus.OnHold) return (false, "Only held work orders can be resumed.");

        var oldStatus = entity.Status;
        entity.Status = WorkOrderStatus.InProgress;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        await _auditTrail.LogAsync("Production", "Resume", "WorkOrder", id.ToString(), performedBy,
            new { Status = oldStatus.ToString() }, new { Status = WorkOrderStatus.InProgress.ToString() }, cancellationToken: cancellationToken);
        return (true, $"Work order {entity.WorkOrderNumber} resumed.");
    }

    public async Task<(bool Success, string Message)> CompleteAsync(int id, CompleteWorkOrderViewModel model, string performedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _context.WorkOrders.Include(x => x.ProductionPlan).ThenInclude(x => x!.Product).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return (false, "Work order not found.");
        if (entity.Status != WorkOrderStatus.InProgress) return (false, $"Cannot complete a work order in '{entity.Status}' status.");

        var product = entity.ProductionPlan?.Product;
        if (product is null) return (false, "Associated product not found.");

        var batchNumber = string.IsNullOrWhiteSpace(model.BatchNumber)
            ? $"BAT-{entity.WorkOrderNumber}-{DateTime.UtcNow:yyyyMMddHHmmss}"
            : model.BatchNumber.Trim();

        var oldStatus = entity.Status;
        entity.Status = WorkOrderStatus.Completed;
        entity.OutputQuantity = model.ProducedQuantity;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        _context.ProductionBatches.Add(new ProductionBatch
        {
            BatchNumber = batchNumber,
            WorkOrderId = entity.Id,
            ProductId = product.Id,
            TraceabilityCode = batchNumber,
            RawMaterialConsumed = model.RawMaterialConsumed,
            ProducedQuantity = model.ProducedQuantity,
            ProducedAt = model.ProducedAt,
            ExpirationDate = model.ExpirationDate,
            Notes = model.Notes
        });

        await _context.SaveChangesAsync(cancellationToken);

        await _auditTrail.LogAsync("Production", "Complete", "WorkOrder", id.ToString(), performedBy,
            new { Status = oldStatus.ToString(), OutputQuantity = 0m },
            new { Status = WorkOrderStatus.Completed.ToString(), OutputQuantity = model.ProducedQuantity, BatchNumber = batchNumber },
            cancellationToken: cancellationToken);

        return (true, $"Work order {entity.WorkOrderNumber} completed. Batch {batchNumber} created.");
    }

    public async Task<(bool Success, string Message)> CancelAsync(int id, string performedBy, CancellationToken cancellationToken = default)
    {
        var entity = await _context.WorkOrders.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return (false, "Work order not found.");
        if (entity.Status == WorkOrderStatus.Completed) return (false, "Cannot cancel a completed work order.");
        if (entity.Status == WorkOrderStatus.Cancelled) return (false, "Work order is already cancelled.");

        var oldStatus = entity.Status;
        entity.Status = WorkOrderStatus.Cancelled;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        await _auditTrail.LogAsync("Production", "Cancel", "WorkOrder", id.ToString(), performedBy,
            new { Status = oldStatus.ToString() }, new { Status = WorkOrderStatus.Cancelled.ToString() }, cancellationToken: cancellationToken);
        return (true, $"Work order {entity.WorkOrderNumber} cancelled.");
    }

    private async Task<List<ProductionPlanOptionViewModel>> GetProductionPlanOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ProductionPlans.AsNoTracking().Include(x => x.Product).OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new ProductionPlanOptionViewModel { Id = x.Id, PlanCode = x.PlanCode, ProductName = x.Product!.Name })
            .ToListAsync(cancellationToken);
    }

    private static string NormalizeSort(string? sort)
    {
        return sort?.Trim().ToLowerInvariant() switch
        {
            "number" => "number",
            "number_desc" => "number_desc",
            "date" => "date",
            "date_desc" => "date_desc",
            "quantity" => "quantity",
            "quantity_desc" => "quantity_desc",
            _ => "newest"
        };
    }

    private static WorkOrderFormViewModel MapForm(WorkOrder entity)
    {
        return new WorkOrderFormViewModel
        {
            Id = entity.Id,
            WorkOrderNumber = entity.WorkOrderNumber,
            ProductionPlanId = entity.ProductionPlanId,
            ScheduledDate = entity.ScheduledDate,
            Quantity = entity.Quantity,
            Status = entity.Status,
            AssignedTo = entity.AssignedTo,
            OutputQuantity = entity.OutputQuantity,
            Notes = entity.Notes
        };
    }

    private static WorkOrderDetailsViewModel MapDetails(WorkOrder entity)
    {
        return new WorkOrderDetailsViewModel
        {
            Id = entity.Id,
            WorkOrderNumber = entity.WorkOrderNumber,
            ProductionPlanId = entity.ProductionPlanId,
            PlanCode = entity.ProductionPlan?.PlanCode ?? "Unknown",
            ScheduledDate = entity.ScheduledDate,
            Quantity = entity.Quantity,
            Status = entity.Status.ToString(),
            StatusTone = entity.Status switch
            {
                WorkOrderStatus.Completed => "success",
                WorkOrderStatus.InProgress => "primary",
                WorkOrderStatus.Planned => "info",
                WorkOrderStatus.OnHold => "warning",
                WorkOrderStatus.Cancelled => "danger",
                _ => "secondary"
            },
            AssignedTo = entity.AssignedTo,
            OutputQuantity = entity.OutputQuantity,
            Notes = entity.Notes,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc
        };
    }

    private static void ApplyForm(WorkOrder entity, WorkOrderFormViewModel model)
    {
        entity.WorkOrderNumber = model.WorkOrderNumber.Trim();
        entity.ProductionPlanId = model.ProductionPlanId;
        entity.ScheduledDate = model.ScheduledDate;
        entity.Quantity = model.Quantity;
        entity.Status = model.Status;
        entity.AssignedTo = model.AssignedTo.Trim();
        entity.OutputQuantity = model.OutputQuantity;
        entity.Notes = model.Notes.Trim();
    }
}

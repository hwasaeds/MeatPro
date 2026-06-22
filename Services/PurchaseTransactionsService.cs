using MeatPro.Data;
using MeatPro.Models;
using MeatPro.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace MeatPro.Services;

public interface IPurchaseTransactionService
{
    Task<PurchaseTransactionIndexViewModel> BuildIndexAsync(string? search, string? sort, string? statusFilter, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PurchaseTransactionDetailsViewModel?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<PurchaseTransactionFormViewModel?> GetEditAsync(int id, CancellationToken cancellationToken = default);
    Task<PurchaseTransactionDetailsViewModel?> GetDeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> IsPurchaseNumberInUseAsync(string purchaseNumber, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(PurchaseTransactionFormViewModel model, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, PurchaseTransactionFormViewModel model, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class PurchaseTransactionService : IPurchaseTransactionService
{
    private const int DefaultPageSize = 10;
    private readonly ApplicationDbContext _context;
    private readonly IAuditTrailService _auditTrail;

    public PurchaseTransactionService(ApplicationDbContext context, IAuditTrailService auditTrail)
    {
        _context = context;
        _auditTrail = auditTrail;
    }

    public async Task<PurchaseTransactionIndexViewModel> BuildIndexAsync(string? search, string? sort, string? statusFilter, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        pageSize = pageSize <= 0 ? DefaultPageSize : pageSize;
        var normalizedSearch = search?.Trim() ?? string.Empty;
        var normalizedSort = NormalizeSort(sort);

        IQueryable<PurchaseTransaction> query = _context.PurchaseTransactions.AsNoTracking().Include(x => x.Supplier);

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(x =>
                EF.Functions.Like(x.PurchaseNumber, $"%{normalizedSearch}%") ||
                EF.Functions.Like(x.Supplier!.Name, $"%{normalizedSearch}%") ||
                EF.Functions.Like(x.Notes, $"%{normalizedSearch}%"));
        }

        if (!string.IsNullOrWhiteSpace(statusFilter) && Enum.TryParse<PurchaseStatus>(statusFilter, true, out var parsedStatus))
        {
            query = query.Where(x => x.Status == parsedStatus);
        }

        query = normalizedSort switch
        {
            "number" => query.OrderBy(x => x.PurchaseNumber),
            "number_desc" => query.OrderByDescending(x => x.PurchaseNumber),
            "amount" => query.OrderBy(x => x.TotalAmount),
            "amount_desc" => query.OrderByDescending(x => x.TotalAmount),
            "date" => query.OrderBy(x => x.PurchasedOn),
            "date_desc" => query.OrderByDescending(x => x.PurchasedOn),
            _ => query.OrderByDescending(x => x.PurchasedOn)
        };

        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        var pageEntities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var pageItems = pageEntities.Select(x => new PurchaseTransactionListItemViewModel
        {
            Id = x.Id,
            PurchaseNumber = x.PurchaseNumber,
            SupplierName = x.Supplier?.Name ?? "Unknown",
            PurchasedOn = x.PurchasedOn,
            TotalAmount = x.TotalAmount,
            Status = x.Status.ToString(),
            StatusTone = x.Status switch
            {
                PurchaseStatus.Received => "success",
                PurchaseStatus.PartiallyReceived => "warning",
                PurchaseStatus.Ordered => "primary",
                PurchaseStatus.Cancelled => "danger",
                _ => "secondary"
            }
        }).ToList();

        var totalSpend = await query.SumAsync(x => x.TotalAmount, cancellationToken);
        var receivedCount = await query.CountAsync(x => x.Status == PurchaseStatus.Received, cancellationToken);
        var pendingCount = await query.CountAsync(x => x.Status == PurchaseStatus.Ordered || x.Status == PurchaseStatus.PartiallyReceived, cancellationToken);

        return new PurchaseTransactionIndexViewModel
        {
            Search = normalizedSearch,
            Sort = normalizedSort,
            StatusFilter = statusFilter,
            Items = pageItems,
            TotalItems = totalItems,
            PageNumber = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            TotalSpend = totalSpend,
            ReceivedCount = receivedCount,
            PendingCount = pendingCount
        };
    }

    public async Task<PurchaseTransactionDetailsViewModel?> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.PurchaseTransactions.AsNoTracking().Include(x => x.Supplier).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : MapDetails(entity);
    }

    public async Task<PurchaseTransactionFormViewModel?> GetEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.PurchaseTransactions.AsNoTracking().Include(x => x.Supplier).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return null;

        var model = MapForm(entity);
        model.Suppliers = await GetSupplierOptionsAsync(cancellationToken);
        return model;
    }

    public async Task<PurchaseTransactionDetailsViewModel?> GetDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await GetDetailsAsync(id, cancellationToken);
    }

    public async Task<bool> IsPurchaseNumberInUseAsync(string purchaseNumber, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalized = purchaseNumber.Trim();
        return await _context.PurchaseTransactions.AsNoTracking().AnyAsync(x => x.PurchaseNumber == normalized && x.Id != (excludeId ?? 0), cancellationToken);
    }

    public async Task<int> CreateAsync(PurchaseTransactionFormViewModel model, CancellationToken cancellationToken = default)
    {
        var entity = new PurchaseTransaction();
        ApplyForm(entity, model);
        _context.PurchaseTransactions.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditTrail.LogAsync("Procurement", "Create", "PurchaseTransaction", entity.Id.ToString(), null,
            null, new { entity.PurchaseNumber, entity.TotalAmount }, cancellationToken: cancellationToken);

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(int id, PurchaseTransactionFormViewModel model, CancellationToken cancellationToken = default)
    {
        var entity = await _context.PurchaseTransactions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return false;

        var oldValues = new { entity.PurchaseNumber, entity.Status, entity.TotalAmount };
        ApplyForm(entity, model);
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        await _auditTrail.LogAsync("Procurement", "Update", "PurchaseTransaction", id.ToString(), null,
            oldValues, new { entity.PurchaseNumber, entity.Status, entity.TotalAmount }, cancellationToken: cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.PurchaseTransactions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return false;

        var oldValues = new { entity.PurchaseNumber };
        _context.PurchaseTransactions.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditTrail.LogAsync("Procurement", "Delete", "PurchaseTransaction", id.ToString(), null,
            oldValues, null, cancellationToken: cancellationToken);

        return true;
    }

    private async Task<List<SupplierOptionViewModel>> GetSupplierOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Suppliers.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new SupplierOptionViewModel { Id = x.Id, Name = x.Name })
            .ToListAsync(cancellationToken);
    }

    private static string NormalizeSort(string? sort)
    {
        return sort?.Trim().ToLowerInvariant() switch
        {
            "number" => "number",
            "number_desc" => "number_desc",
            "amount" => "amount",
            "amount_desc" => "amount_desc",
            "date" => "date",
            "date_desc" => "date_desc",
            _ => "newest"
        };
    }

    private static PurchaseTransactionFormViewModel MapForm(PurchaseTransaction entity)
    {
        return new PurchaseTransactionFormViewModel
        {
            Id = entity.Id,
            PurchaseNumber = entity.PurchaseNumber,
            SupplierId = entity.SupplierId,
            PurchasedOn = entity.PurchasedOn,
            TotalAmount = entity.TotalAmount,
            Status = entity.Status,
            ReceivedOn = entity.ReceivedOn,
            Notes = entity.Notes
        };
    }

    private static PurchaseTransactionDetailsViewModel MapDetails(PurchaseTransaction entity)
    {
        return new PurchaseTransactionDetailsViewModel
        {
            Id = entity.Id,
            PurchaseNumber = entity.PurchaseNumber,
            SupplierId = entity.SupplierId,
            SupplierName = entity.Supplier?.Name ?? "Unknown",
            PurchasedOn = entity.PurchasedOn,
            Status = entity.Status.ToString(),
            StatusTone = entity.Status switch
            {
                PurchaseStatus.Received => "success",
                PurchaseStatus.PartiallyReceived => "warning",
                PurchaseStatus.Ordered => "primary",
                PurchaseStatus.Cancelled => "danger",
                _ => "secondary"
            },
            TotalAmount = entity.TotalAmount,
            ReceivedOn = entity.ReceivedOn,
            Notes = entity.Notes,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc
        };
    }

    private static void ApplyForm(PurchaseTransaction entity, PurchaseTransactionFormViewModel model)
    {
        entity.PurchaseNumber = model.PurchaseNumber.Trim();
        entity.SupplierId = model.SupplierId;
        entity.PurchasedOn = model.PurchasedOn;
        entity.TotalAmount = model.TotalAmount;
        entity.Status = model.Status;
        entity.ReceivedOn = model.ReceivedOn;
        entity.Notes = model.Notes.Trim();
    }
}

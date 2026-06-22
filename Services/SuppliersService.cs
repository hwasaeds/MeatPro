using MeatPro.Data;
using MeatPro.Models;
using MeatPro.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace MeatPro.Services;

public interface ISupplierService
{
    Task<IReadOnlyList<Supplier>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<SupplierIndexViewModel> BuildIndexAsync(string? search, string? sort, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<SupplierDetailsViewModel?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<SupplierFormViewModel?> GetEditAsync(int id, CancellationToken cancellationToken = default);
    Task<SupplierDetailsViewModel?> GetDeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> IsNameInUseAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(SupplierFormViewModel model, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, SupplierFormViewModel model, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class SupplierService : ISupplierService
{
    private const int DefaultPageSize = 10;
    private readonly ApplicationDbContext _context;
    private readonly IAuditTrailService _auditTrail;

    public SupplierService(ApplicationDbContext context, IAuditTrailService auditTrail)
    {
        _context = context;
        _auditTrail = auditTrail;
    }

    public async Task<IReadOnlyList<Supplier>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Suppliers.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public async Task<SupplierIndexViewModel> BuildIndexAsync(string? search, string? sort, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        pageSize = pageSize <= 0 ? DefaultPageSize : pageSize;
        var normalizedSearch = search?.Trim() ?? string.Empty;
        var normalizedSort = NormalizeSort(sort);

        IQueryable<Supplier> query = _context.Suppliers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(x =>
                EF.Functions.Like(x.Name, $"%{normalizedSearch}%") ||
                EF.Functions.Like(x.ContactPerson, $"%{normalizedSearch}%") ||
                EF.Functions.Like(x.Phone, $"%{normalizedSearch}%") ||
                EF.Functions.Like(x.Email, $"%{normalizedSearch}%"));
        }

        query = normalizedSort switch
        {
            "name" => query.OrderBy(x => x.Name),
            "name_desc" => query.OrderByDescending(x => x.Name),
            "contact" => query.OrderBy(x => x.ContactPerson),
            "contact_desc" => query.OrderByDescending(x => x.ContactPerson),
            _ => query.OrderByDescending(x => x.CreatedAtUtc)
        };

        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        var pageEntities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var pageItems = pageEntities.Select(x => new SupplierListItemViewModel
        {
            Id = x.Id,
            Name = x.Name,
            ContactPerson = x.ContactPerson,
            Phone = x.Phone,
            Email = x.Email,
            IsActive = x.IsActive,
            Status = x.IsActive ? "Active" : "Inactive",
            StatusTone = x.IsActive ? "success" : "secondary"
        }).ToList();

        var activeCount = await query.CountAsync(x => x.IsActive, cancellationToken);
        var inactiveCount = totalItems - activeCount;

        return new SupplierIndexViewModel
        {
            Search = normalizedSearch,
            Sort = normalizedSort,
            Items = pageItems,
            TotalItems = totalItems,
            PageNumber = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            ActiveCount = activeCount,
            InactiveCount = inactiveCount
        };
    }

    public async Task<SupplierDetailsViewModel?> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Suppliers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : MapDetails(entity);
    }

    public async Task<SupplierFormViewModel?> GetEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Suppliers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : MapForm(entity);
    }

    public async Task<SupplierDetailsViewModel?> GetDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await GetDetailsAsync(id, cancellationToken);
    }

    public async Task<bool> IsNameInUseAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();
        return await _context.Suppliers.AsNoTracking().AnyAsync(x => x.Name == normalizedName && x.Id != (excludeId ?? 0), cancellationToken);
    }

    public async Task<int> CreateAsync(SupplierFormViewModel model, CancellationToken cancellationToken = default)
    {
        var entity = new Supplier();
        ApplyForm(entity, model);
        _context.Suppliers.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditTrail.LogAsync("Procurement", "Create", "Supplier", entity.Id.ToString(), null,
            null, new { entity.Name, entity.ContactPerson }, cancellationToken: cancellationToken);

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(int id, SupplierFormViewModel model, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Suppliers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return false;

        var oldValues = new { entity.Name, entity.ContactPerson, entity.IsActive };
        ApplyForm(entity, model);
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        await _auditTrail.LogAsync("Procurement", "Update", "Supplier", id.ToString(), null,
            oldValues, new { entity.Name, entity.ContactPerson, entity.IsActive }, cancellationToken: cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Suppliers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return false;

        var oldValues = new { entity.Name };
        _context.Suppliers.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditTrail.LogAsync("Procurement", "Delete", "Supplier", id.ToString(), null,
            oldValues, null, cancellationToken: cancellationToken);

        return true;
    }

    private static string NormalizeSort(string? sort)
    {
        return sort?.Trim().ToLowerInvariant() switch
        {
            "name" => "name",
            "name_desc" => "name_desc",
            "contact" => "contact",
            "contact_desc" => "contact_desc",
            _ => "newest"
        };
    }

    private static SupplierFormViewModel MapForm(Supplier entity)
    {
        return new SupplierFormViewModel
        {
            Id = entity.Id,
            Name = entity.Name,
            ContactPerson = entity.ContactPerson,
            Phone = entity.Phone,
            Email = entity.Email,
            Address = entity.Address,
            IsActive = entity.IsActive
        };
    }

    private static SupplierDetailsViewModel MapDetails(Supplier entity)
    {
        return new SupplierDetailsViewModel
        {
            Id = entity.Id,
            Name = entity.Name,
            ContactPerson = entity.ContactPerson,
            Phone = entity.Phone,
            Email = entity.Email,
            Address = entity.Address,
            IsActive = entity.IsActive,
            Status = entity.IsActive ? "Active" : "Inactive",
            StatusTone = entity.IsActive ? "success" : "secondary",
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc
        };
    }

    private static void ApplyForm(Supplier entity, SupplierFormViewModel model)
    {
        entity.Name = model.Name.Trim();
        entity.ContactPerson = model.ContactPerson.Trim();
        entity.Phone = model.Phone.Trim();
        entity.Email = model.Email.Trim();
        entity.Address = model.Address.Trim();
        entity.IsActive = model.IsActive;
    }
}

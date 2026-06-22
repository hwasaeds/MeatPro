using System.Text.Json;
using MeatPro.Data;
using MeatPro.Models;
using Microsoft.EntityFrameworkCore;

namespace MeatPro.Services;

public interface IAuditTrailService
{
    Task LogAsync(string module, string action, string entityName, string entityId, string? username, object? oldValues = null, object? newValues = null, string? ipAddress = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLog>> GetRecentAsync(int count = 20, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLog>> SearchAsync(string? module, string? action, string? username, DateTime? fromDate, DateTime? toDate, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> SearchCountAsync(string? module, string? action, string? username, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetDistinctModulesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetDistinctActionsAsync(CancellationToken cancellationToken = default);
}

public sealed class AuditTrailService : IAuditTrailService
{
    private readonly ApplicationDbContext _context;

    public AuditTrailService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(string module, string action, string entityName, string entityId, string? username, object? oldValues = null, object? newValues = null, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        var log = new AuditLog
        {
            Module = module,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Username = username ?? "system",
            OldValues = oldValues is null ? null : JsonSerializer.Serialize(oldValues),
            NewValues = newValues is null ? null : JsonSerializer.Serialize(newValues),
            IpAddress = ipAddress
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLog>> GetRecentAsync(int count = 20, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs.AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLog>> SearchAsync(string? module, string? action, string? username, DateTime? fromDate, DateTime? toDate, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = BuildSearchQuery(module, action, username, fromDate, toDate);
        pageSize = Math.Clamp(pageSize, 1, 100);
        return await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((Math.Max(1, page) - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> SearchCountAsync(string? module, string? action, string? username, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var query = BuildSearchQuery(module, action, username, fromDate, toDate);
        return await query.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetDistinctModulesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs.AsNoTracking().Select(x => x.Module).Distinct().OrderBy(x => x).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetDistinctActionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs.AsNoTracking().Select(x => x.Action).Distinct().OrderBy(x => x).ToListAsync(cancellationToken);
    }

    private IQueryable<AuditLog> BuildSearchQuery(string? module, string? action, string? username, DateTime? fromDate, DateTime? toDate)
    {
        var query = _context.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(module))
            query = query.Where(x => x.Module == module.Trim());
        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(x => x.Action == action.Trim());
        if (!string.IsNullOrWhiteSpace(username))
            query = query.Where(x => EF.Functions.Like(x.Username, $"%{username.Trim()}%"));
        if (fromDate.HasValue)
            query = query.Where(x => x.CreatedAtUtc >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(x => x.CreatedAtUtc <= toDate.Value);

        return query;
    }
}

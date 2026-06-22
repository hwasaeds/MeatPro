using MeatPro.Data;
using MeatPro.Models;
using Microsoft.EntityFrameworkCore;

namespace MeatPro.Services;

public interface IAlertService
{
    Task GenerateAlertsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationItem>> GetActiveAlertsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationItem>> GetAllNotificationsAsync(string? typeFilter, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(string? typeFilter, CancellationToken cancellationToken = default);
    Task DismissAlertAsync(int id, CancellationToken cancellationToken = default);
    Task ReadAllAsync(CancellationToken cancellationToken = default);
}

public sealed class AlertService : IAlertService
{
    private readonly ApplicationDbContext _context;

    public AlertService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task GenerateAlertsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expiryCutoff = now.AddDays(14);
        var existingActive = await _context.Notifications
            .Where(x => x.ExpiresAtUtc == null || x.ExpiresAtUtc > now)
            .Select(x => x.Title + "|" + x.Message)
            .ToListAsync(cancellationToken);

        bool Exists(string title, string message) =>
            existingActive.Contains(title + "|" + message);

        var rawMaterials = await _context.RawMaterials.AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var rm in rawMaterials)
        {
            if (rm.CurrentStock <= rm.ReorderLevel)
            {
                var title = "Low Stock Alert";
                var message = $"{rm.Name} (SKU: {rm.SKU}) is below reorder level. Current: {rm.CurrentStock:N0}, Reorder: {rm.ReorderLevel:N0}.";
                if (!Exists(title, message))
                {
                    _context.Notifications.Add(new NotificationItem
                    {
                        Title = title,
                        Message = message,
                        Type = NotificationType.Warning,
                        ExpiresAtUtc = now.AddDays(7)
                    });
                }
            }

            if (rm.ExpirationDate is not null && rm.ExpirationDate <= expiryCutoff && rm.CurrentStock > 0)
            {
                var title = "Expiration Alert";
                var message = $"{rm.Name} (SKU: {rm.SKU}) expires on {rm.ExpirationDate:yyyy-MM-dd}. Current stock: {rm.CurrentStock:N0}.";
                if (!Exists(title, message))
                {
                    _context.Notifications.Add(new NotificationItem
                    {
                        Title = title,
                        Message = message,
                        Type = NotificationType.Danger,
                        ExpiresAtUtc = rm.ExpirationDate.Value
                    });
                }
            }
        }

        var finishedGoods = await _context.FinishedGoods.AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var fg in finishedGoods)
        {
            if (fg.CurrentStock <= fg.ReorderLevel)
            {
                var title = "Low Stock Alert";
                var message = $"{fg.Name} (SKU: {fg.SKU}) is below reorder level. Current: {fg.CurrentStock:N0}, Reorder: {fg.ReorderLevel:N0}.";
                if (!Exists(title, message))
                {
                    _context.Notifications.Add(new NotificationItem
                    {
                        Title = title,
                        Message = message,
                        Type = NotificationType.Warning,
                        ExpiresAtUtc = now.AddDays(7)
                    });
                }
            }

            if (fg.ExpirationDate is not null && fg.ExpirationDate <= expiryCutoff && fg.CurrentStock > 0)
            {
                var title = "Expiration Alert";
                var message = $"{fg.Name} (SKU: {fg.SKU}) expires on {fg.ExpirationDate:yyyy-MM-dd}. Current stock: {fg.CurrentStock:N0}.";
                if (!Exists(title, message))
                {
                    _context.Notifications.Add(new NotificationItem
                    {
                        Title = title,
                        Message = message,
                        Type = NotificationType.Danger,
                        ExpiresAtUtc = fg.ExpirationDate.Value
                    });
                }
            }
        }

        var pendingApprovals = await _context.PurchaseTransactions.AsNoTracking()
            .CountAsync(x => x.Status == PurchaseStatus.Draft, cancellationToken);

        if (pendingApprovals > 0)
        {
            var title = "Pending Approval";
            var message = $"{pendingApprovals} purchase transaction(s) are in Draft status and require action.";
            if (!Exists(title, message))
            {
                _context.Notifications.Add(new NotificationItem
                {
                    Title = title,
                    Message = message,
                    Type = NotificationType.Info,
                    ExpiresAtUtc = now.AddDays(3)
                });
            }
        }

        var pendingWorkOrders = await _context.WorkOrders.AsNoTracking()
            .CountAsync(x => x.Status == WorkOrderStatus.Draft, cancellationToken);

        if (pendingWorkOrders > 0)
        {
            var title = "Pending Approval";
            var message = $"{pendingWorkOrders} work order(s) are in Draft status and require action.";
            if (!Exists(title, message))
            {
                _context.Notifications.Add(new NotificationItem
                {
                    Title = title,
                    Message = message,
                    Type = NotificationType.Info,
                    ExpiresAtUtc = now.AddDays(3)
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationItem>> GetActiveAlertsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Notifications.AsNoTracking()
            .Where(x => !x.IsRead && (x.ExpiresAtUtc == null || x.ExpiresAtUtc > now))
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Notifications.AsNoTracking()
            .CountAsync(x => !x.IsRead && (x.ExpiresAtUtc == null || x.ExpiresAtUtc > now), cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationItem>> GetAllNotificationsAsync(string? typeFilter, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Notifications.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(typeFilter) && Enum.TryParse<NotificationType>(typeFilter, true, out var parsedType))
            query = query.Where(x => x.Type == parsedType);

        return await query.OrderByDescending(x => x.CreatedAtUtc)
            .Skip((Math.Max(1, page) - 1) * Math.Clamp(pageSize, 1, 100))
            .Take(Math.Clamp(pageSize, 1, 100))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(string? typeFilter, CancellationToken cancellationToken = default)
    {
        var query = _context.Notifications.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(typeFilter) && Enum.TryParse<NotificationType>(typeFilter, true, out var parsedType))
            query = query.Where(x => x.Type == parsedType);
        return await query.CountAsync(cancellationToken);
    }

    public async Task ReadAllAsync(CancellationToken cancellationToken = default)
    {
        var unread = await _context.Notifications.Where(x => !x.IsRead).ToListAsync(cancellationToken);
        foreach (var item in unread) item.IsRead = true;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DismissAlertAsync(int id, CancellationToken cancellationToken = default)
    {
        var alert = await _context.Notifications.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (alert is not null)
        {
            alert.IsRead = true;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

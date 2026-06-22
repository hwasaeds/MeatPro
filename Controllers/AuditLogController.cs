using MeatPro.Services;
using MeatPro.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeatPro.Controllers;

[Authorize]
public sealed class AuditLogController : Controller
{
    private readonly IAuditTrailService _auditTrail;

    public AuditLogController(IAuditTrailService auditTrail)
    {
        _auditTrail = auditTrail;
    }

    public async Task<IActionResult> Index(string? module, string? action, string? username, DateTime? fromDate, DateTime? toDate, int page = 1, CancellationToken cancellationToken = default)
    {
        var pageSize = 30;
        var items = await _auditTrail.SearchAsync(module, action, username, fromDate, toDate, page, pageSize, cancellationToken);
        var total = await _auditTrail.SearchCountAsync(module, action, username, fromDate, toDate, cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        var modules = await _auditTrail.GetDistinctModulesAsync(cancellationToken);
        var actions = await _auditTrail.GetDistinctActionsAsync(cancellationToken);

        return View(new AuditLogIndexViewModel
        {
            Items = items.Select(x => new AuditLogItemViewModel
            {
                Id = x.Id,
                Module = x.Module,
                Action = x.Action,
                EntityName = x.EntityName,
                EntityId = x.EntityId,
                Username = x.Username,
                OldValues = x.OldValues,
                NewValues = x.NewValues,
                Timestamp = x.CreatedAtUtc
            }).ToList(),
            Module = module,
            Action = action,
            Username = username,
            FromDate = fromDate,
            ToDate = toDate,
            Page = page,
            PageSize = pageSize,
            TotalItems = total,
            TotalPages = totalPages,
            AvailableModules = modules,
            AvailableActions = actions
        });
    }
}

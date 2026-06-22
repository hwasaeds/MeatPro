using MeatPro.Models;
using MeatPro.Services;
using MeatPro.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeatPro.Controllers;

[Authorize]
public sealed class NotificationsController : Controller
{
    private readonly IAlertService _alertService;

    public NotificationsController(IAlertService alertService)
    {
        _alertService = alertService;
    }

    public async Task<IActionResult> Index(string? type, int page = 1, CancellationToken cancellationToken = default)
    {
        var pageSize = 20;
        var items = await _alertService.GetAllNotificationsAsync(type, page, pageSize, cancellationToken);
        var total = await _alertService.GetTotalCountAsync(type, cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));

        return View(new NotificationIndexViewModel
        {
            Items = items,
            TypeFilter = type,
            Page = page,
            PageSize = pageSize,
            TotalItems = total,
            TotalPages = totalPages
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Dismiss(int id, CancellationToken cancellationToken = default)
    {
        await _alertService.DismissAlertAsync(id, cancellationToken);
        TempData["SuccessMessage"] = "Notification dismissed.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReadAll(CancellationToken cancellationToken = default)
    {
        await _alertService.ReadAllAsync(cancellationToken);
        TempData["SuccessMessage"] = "All notifications marked as read.";
        return RedirectToAction(nameof(Index));
    }
}

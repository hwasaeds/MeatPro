using MeatPro.Models;

namespace MeatPro.ViewModels;

public sealed class NotificationIndexViewModel
{
    public IReadOnlyList<NotificationItem> Items { get; set; } = Array.Empty<NotificationItem>();
    public string? TypeFilter { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalItems { get; set; }
    public int TotalPages { get; set; } = 1;
}

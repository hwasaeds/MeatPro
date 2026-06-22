namespace MeatPro.ViewModels;

public sealed class AuditLogIndexViewModel
{
    public IReadOnlyList<AuditLogItemViewModel> Items { get; set; } = Array.Empty<AuditLogItemViewModel>();
    public string? Module { get; set; }
    public string? Action { get; set; }
    public string? Username { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 30;
    public int TotalItems { get; set; }
    public int TotalPages { get; set; } = 1;
    public IReadOnlyList<string> AvailableModules { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> AvailableActions { get; set; } = Array.Empty<string>();
}

public sealed class AuditLogItemViewModel
{
    public int Id { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public DateTime Timestamp { get; set; }
}

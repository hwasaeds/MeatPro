using System.ComponentModel.DataAnnotations;

namespace MeatPro.ViewModels;

public sealed class LoginViewModel
{
    [Required]
    [Display(Name = "Email or Username")]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}



public sealed class DashboardViewModel
{
    public int TotalRawMaterials { get; set; }
    public int LowStockMaterials { get; set; }
    public int ActiveWorkOrders { get; set; }
    public int FinishedGoodsCount { get; set; }
    public decimal MonthlyProductionOutput { get; set; }
    public int TotalSuppliers { get; set; }
    public IReadOnlyList<MetricCardViewModel> MetricCards { get; set; } = Array.Empty<MetricCardViewModel>();
    public IReadOnlyList<ActivityItemViewModel> RecentActivities { get; set; } = Array.Empty<ActivityItemViewModel>();
    public ChartSeriesViewModel ProductionOutputTrend { get; set; } = new();
    public ChartSeriesViewModel InventoryMovementChart { get; set; } = new();
    public ChartSeriesViewModel TopProducedProducts { get; set; } = new();
    public ChartSeriesViewModel MaterialConsumption { get; set; } = new();
    public IReadOnlyList<AlertItemViewModel> Alerts { get; set; } = Array.Empty<AlertItemViewModel>();
}

public sealed class MetricCardViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Tone { get; set; } = string.Empty;
}

public sealed class ActivityItemViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TimeAgo { get; set; } = string.Empty;
    public string Badge { get; set; } = string.Empty;
    public string BadgeTone { get; set; } = string.Empty;
}

public sealed class AlertItemViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Tone { get; set; } = string.Empty;
}

public sealed class ChartSeriesViewModel
{
    public List<string> Labels { get; set; } = [];
    public List<decimal> Values { get; set; } = [];
}

public sealed class ModulePageViewModel
{
    public string ModuleName { get; set; } = "Inventory";
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string SearchPlaceholder { get; set; } = string.Empty;
    public string PrimaryActionText { get; set; } = string.Empty;
    public string PrimaryActionUrl { get; set; } = "#";
    public bool ShowExportActions { get; set; }
    public IReadOnlyList<MetricCardViewModel> Metrics { get; set; } = Array.Empty<MetricCardViewModel>();
    public IReadOnlyList<ModuleColumnViewModel> Columns { get; set; } = Array.Empty<ModuleColumnViewModel>();
    public IReadOnlyList<ModuleRowViewModel> Rows { get; set; } = Array.Empty<ModuleRowViewModel>();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalItems { get; set; }
}

public sealed class ModuleColumnViewModel
{
    public string Header { get; set; } = string.Empty;
    public bool IsNumeric { get; set; }
}

public sealed class ModuleRowViewModel
{
    public IReadOnlyList<string> Cells { get; set; } = Array.Empty<string>();
    public string Badge { get; set; } = string.Empty;
    public string BadgeTone { get; set; } = string.Empty;
}

public sealed class StockOperationViewModel
{
    public int RawMaterialId { get; set; }
    public string RawMaterialName { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }

    [Required]
    [Range(0.001, 9999999)]
    public decimal Quantity { get; set; }

    [Display(Name = "Reference Number")]
    public string? ReferenceNumber { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}

public sealed class ReleaseToProductionViewModel
{
    public int RawMaterialId { get; set; }
    public string RawMaterialName { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }

    [Required]
    [Range(0.001, 9999999)]
    public decimal Quantity { get; set; }

    [Required]
    [Display(Name = "Work Order Number")]
    public string WorkOrderNumber { get; set; } = string.Empty;
}

public sealed class FinishedGoodAdjustmentViewModel
{
    public int FinishedGoodId { get; set; }
    public string FinishedGoodName { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }

    [Required]
    [Range(0, 9999999)]
    [Display(Name = "New Stock Quantity")]
    public decimal NewQuantity { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }
}

public sealed class ReceivePurchaseViewModel
{
    public int PurchaseTransactionId { get; set; }
    public string PurchaseNumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }

    [Required]
    [Display(Name = "Received On")]
    public DateTime ReceivedOn { get; set; } = DateTime.UtcNow;

    [StringLength(500)]
    public string? Notes { get; set; }
}
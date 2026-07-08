using System.ComponentModel.DataAnnotations;

namespace MeatPro.ViewModels;

public sealed class InventoryReportViewModel
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? StatusFilter { get; set; }
    public int TotalMaterials { get; set; }
    public decimal TotalStockValue { get; set; }
    public int LowStockCount { get; set; }
    public int ExpiringCount { get; set; }
    public IReadOnlyList<InventoryReportItem> Items { get; set; } = Array.Empty<InventoryReportItem>();
}

public sealed class InventoryReportItem
{
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal UnitValue { get; set; }
    public decimal TotalValue { get; set; }
    public string StorageLocation { get; set; } = string.Empty;
    public DateTime? ExpirationDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class ProductionReportViewModel
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? StatusFilter { get; set; }
    public int TotalWorkOrders { get; set; }
    public int CompletedOrders { get; set; }
    public decimal TotalProduced { get; set; }
    public decimal TotalConsumed { get; set; }
    public IReadOnlyList<ProductionReportItem> Items { get; set; } = Array.Empty<ProductionReportItem>();
}

public sealed class ProductionReportItem
{
    public string BatchNumber { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string WorkOrderNumber { get; set; } = string.Empty;
    public string PlanCode { get; set; } = string.Empty;
    public decimal ProducedQuantity { get; set; }
    public decimal RawMaterialConsumed { get; set; }
    public decimal Yield => ProducedQuantity > 0 ? Math.Round((ProducedQuantity / (RawMaterialConsumed > 0 ? RawMaterialConsumed : 1)) * 100, 1) : 0;
    public DateTime ProducedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class ProcurementReportViewModel
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? StatusFilter { get; set; }
    public int TotalPurchases { get; set; }
    public decimal TotalSpend { get; set; }
    public int PendingCount { get; set; }
    public int ReceivedCount { get; set; }
    public IReadOnlyList<ProcurementReportItem> Items { get; set; } = Array.Empty<ProcurementReportItem>();
}

public sealed class ProcurementReportItem
{
    public string PurchaseNumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public DateTime PurchasedOn { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ReceivedOn { get; set; }
}

public sealed class SalesReportViewModel
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? StatusFilter { get; set; }
    public int TotalItems { get; set; }
    public decimal TotalPotentialRevenue { get; set; }
    public decimal TotalStockQuantity { get; set; }
    public int LowStockCount { get; set; }
    public int ExpiringCount { get; set; }
    public IReadOnlyList<SalesReportItem> Items { get; set; } = Array.Empty<SalesReportItem>();
}

public sealed class SalesReportItem
{
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalValue { get; set; }
    public string StorageLocation { get; set; } = string.Empty;
    public DateTime? ExpirationDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

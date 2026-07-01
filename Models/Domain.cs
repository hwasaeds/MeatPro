using Microsoft.AspNetCore.Identity;

namespace MeatPro.Models;

public abstract class EntityBase
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
}

public static class AppRoles
{
    public const string SystemAdministrator = "System Administrator";
    public const string ProductionManager = "Production Manager";
    public const string InventoryPersonnel = "Inventory Personnel";
    public const string ProcurementManager = "Procurement Manager";

    public static readonly string[] All =
    {
        SystemAdministrator,
        ProductionManager,
        InventoryPersonnel,
        ProcurementManager
    };
}

public enum InventoryMovementType
{
    StockIn = 1,
    StockOut = 2,
    Adjustment = 3,
    ReleaseToProduction = 4
}

public enum ProductionPlanStatus
{
    Draft = 1,
    Approved = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5
}

public enum WorkOrderStatus
{
    Draft = 1,
    Planned = 2,
    InProgress = 3,
    OnHold = 4,
    Completed = 5,
    Cancelled = 6
}

public enum PurchaseStatus
{
    Draft = 1,
    Ordered = 2,
    PartiallyReceived = 3,
    Received = 4,
    Cancelled = 5
}

public enum NotificationType
{
    Info = 1,
    Success = 2,
    Warning = 3,
    Danger = 4
}

public sealed class ProductCategory : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class Product : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int? ProductCategoryId { get; set; }
    public ProductCategory? ProductCategory { get; set; }
    public string Description { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = "kg";
    public decimal StandardYield { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal SellingPrice { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class RawMaterial : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = "kg";
    public decimal CurrentStock { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal UnitCost { get; set; }
    public string Location { get; set; } = string.Empty;
    public DateTime? ExpirationDate { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class FinishedGood : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime? ExpirationDate { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal UnitPrice { get; set; }
    public string StorageLocation { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class StockMovement : EntityBase
{
    public InventoryMovementType MovementType { get; set; }
    public string ItemType { get; set; } = "Raw Material";
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime MovementDate { get; set; } = DateTime.UtcNow;
    public string PerformedBy { get; set; } = string.Empty;
    public bool IsApproved { get; set; } = true;
}

public sealed class ProductionPlan : EntityBase
{
    public string PlanCode { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public decimal PlannedQuantity { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public ProductionPlanStatus Status { get; set; } = ProductionPlanStatus.Draft;
    public string Notes { get; set; } = string.Empty;
    public string CreatedByUser { get; set; } = string.Empty;
}

public sealed class WorkOrder : EntityBase
{
    public string WorkOrderNumber { get; set; } = string.Empty;
    public int ProductionPlanId { get; set; }
    public ProductionPlan? ProductionPlan { get; set; }
    public DateTime ScheduledDate { get; set; }
    public decimal Quantity { get; set; }
    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Draft;
    public string AssignedTo { get; set; } = string.Empty;
    public decimal OutputQuantity { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class ProductionBatch : EntityBase
{
    public string BatchNumber { get; set; } = string.Empty;
    public int WorkOrderId { get; set; }
    public WorkOrder? WorkOrder { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public string TraceabilityCode { get; set; } = string.Empty;
    public decimal RawMaterialConsumed { get; set; }
    public decimal ProducedQuantity { get; set; }
    public DateTime ProducedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpirationDate { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class Supplier : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class PurchaseTransaction : EntityBase
{
    public string PurchaseNumber { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public DateTime PurchasedOn { get; set; } = DateTime.UtcNow;
    public PurchaseStatus Status { get; set; } = PurchaseStatus.Draft;
    public decimal TotalAmount { get; set; }
    public DateTime? ReceivedOn { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class NotificationItem : EntityBase
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Info;
    public bool IsRead { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
}

public sealed class AuditLog : EntityBase
{
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
}
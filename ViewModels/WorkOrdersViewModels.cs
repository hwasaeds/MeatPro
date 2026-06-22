using System.ComponentModel.DataAnnotations;
using MeatPro.Models;

namespace MeatPro.ViewModels;

public sealed class WorkOrderIndexViewModel
{
    public string Search { get; set; } = string.Empty;
    public string Sort { get; set; } = "newest";
    public string? StatusFilter { get; set; }
    public IReadOnlyList<WorkOrderListItemViewModel> Items { get; set; } = Array.Empty<WorkOrderListItemViewModel>();
    public int TotalItems { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages { get; set; } = 1;
    public int OpenCount { get; set; }
    public int CompletedCount { get; set; }
}

public sealed class WorkOrderListItemViewModel
{
    public int Id { get; set; }
    public string WorkOrderNumber { get; set; } = string.Empty;
    public string PlanCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public DateTime ScheduledDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal OutputQuantity { get; set; }
    public string AssignedTo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusTone { get; set; } = string.Empty;
}

public sealed class WorkOrderDetailsViewModel
{
    public int Id { get; set; }
    public string WorkOrderNumber { get; set; } = string.Empty;
    public int ProductionPlanId { get; set; }
    public string PlanCode { get; set; } = string.Empty;
    public DateTime ScheduledDate { get; set; }
    public decimal Quantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusTone { get; set; } = string.Empty;
    public string AssignedTo { get; set; } = string.Empty;
    public decimal OutputQuantity { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class WorkOrderFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(60)]
    [Display(Name = "Work Order Number")]
    public string WorkOrderNumber { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Production Plan")]
    public int ProductionPlanId { get; set; }

    [Required]
    [Display(Name = "Scheduled Date")]
    public DateTime ScheduledDate { get; set; } = DateTime.UtcNow.Date;

    [Required]
    [Range(0, 9999999)]
    public decimal Quantity { get; set; }

    [Required]
    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Draft;

    [StringLength(200)]
    [Display(Name = "Assigned To")]
    public string AssignedTo { get; set; } = string.Empty;

    [Range(0, 9999999)]
    [Display(Name = "Output Quantity")]
    public decimal OutputQuantity { get; set; }

    [StringLength(2000)]
    public string Notes { get; set; } = string.Empty;

    public IReadOnlyList<ProductionPlanOptionViewModel> ProductionPlans { get; set; } = Array.Empty<ProductionPlanOptionViewModel>();
}

public sealed class ProductionPlanOptionViewModel
{
    public int Id { get; set; }
    public string PlanCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
}

public sealed class CompleteWorkOrderViewModel
{
    [Required]
    [Range(0, 9999999)]
    [Display(Name = "Produced Quantity")]
    public decimal ProducedQuantity { get; set; }

    [StringLength(100)]
    [Display(Name = "Batch Number")]
    public string BatchNumber { get; set; } = string.Empty;

    [Display(Name = "Production Date")]
    public DateTime ProducedAt { get; set; } = DateTime.UtcNow;

    [Display(Name = "Raw Material Consumed")]
    [Range(0, 9999999)]
    public decimal RawMaterialConsumed { get; set; }

    [Display(Name = "Expiration Date")]
    public DateTime? ExpirationDate { get; set; }

    [StringLength(2000)]
    public string Notes { get; set; } = string.Empty;
}

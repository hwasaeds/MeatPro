using System.ComponentModel.DataAnnotations;
using MeatPro.Models;

namespace MeatPro.ViewModels;

public sealed class ProductionBatchIndexViewModel
{
    public string Search { get; set; } = string.Empty;
    public string Sort { get; set; } = "newest";
    public IReadOnlyList<ProductionBatchListItemViewModel> Items { get; set; } = Array.Empty<ProductionBatchListItemViewModel>();
    public int TotalItems { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages { get; set; } = 1;
    public int ActiveBatchCount { get; set; }
    public int ExpiringCount { get; set; }
    public decimal TotalProduced { get; set; }
}

public sealed class ProductionBatchListItemViewModel
{
    public int Id { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public string WorkOrderNumber { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string TraceabilityCode { get; set; } = string.Empty;
    public decimal ProducedQuantity { get; set; }
    public DateTime ProducedAt { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusTone { get; set; } = string.Empty;
}

public sealed class ProductionBatchDetailsViewModel
{
    public int Id { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public int WorkOrderId { get; set; }
    public string WorkOrderNumber { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string TraceabilityCode { get; set; } = string.Empty;
    public decimal RawMaterialConsumed { get; set; }
    public decimal ProducedQuantity { get; set; }
    public DateTime ProducedAt { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusTone { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class ProductionBatchFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(60)]
    [Display(Name = "Batch Number")]
    public string BatchNumber { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Work Order")]
    public int WorkOrderId { get; set; }

    [Required]
    [Display(Name = "Product")]
    public int ProductId { get; set; }

    [StringLength(120)]
    [Display(Name = "Traceability Code")]
    public string TraceabilityCode { get; set; } = string.Empty;

    [Range(0, 9999999)]
    [Display(Name = "Raw Material Consumed")]
    public decimal RawMaterialConsumed { get; set; }

    [Required]
    [Range(0, 9999999)]
    [Display(Name = "Produced Quantity")]
    public decimal ProducedQuantity { get; set; }

    [Required]
    [Display(Name = "Produced At")]
    public DateTime ProducedAt { get; set; } = DateTime.UtcNow;

    [Display(Name = "Expiration Date")]
    public DateTime? ExpirationDate { get; set; }

    [StringLength(2000)]
    public string Notes { get; set; } = string.Empty;

    public IReadOnlyList<WorkOrderOptionViewModel> WorkOrders { get; set; } = Array.Empty<WorkOrderOptionViewModel>();
    public IReadOnlyList<ProductOptionViewModel> Products { get; set; } = Array.Empty<ProductOptionViewModel>();
}

public sealed class WorkOrderOptionViewModel
{
    public int Id { get; set; }
    public string WorkOrderNumber { get; set; } = string.Empty;
    public string? ProductName { get; set; }
}

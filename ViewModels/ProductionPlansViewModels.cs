using System.ComponentModel.DataAnnotations;
using MeatPro.Models;

namespace MeatPro.ViewModels;

public sealed class ProductionPlanIndexViewModel
{
    public string Search { get; set; } = string.Empty;
    public string Sort { get; set; } = "newest";
    public string? StatusFilter { get; set; }
    public IReadOnlyList<ProductionPlanListItemViewModel> Items { get; set; } = Array.Empty<ProductionPlanListItemViewModel>();
    public int TotalItems { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages { get; set; } = 1;
    public int ActivePlanCount { get; set; }
    public int DraftCount { get; set; }
    public decimal TotalPlannedQuantity { get; set; }
}

public sealed class ProductionPlanListItemViewModel
{
    public int Id { get; set; }
    public string PlanCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal PlannedQuantity { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusTone { get; set; } = string.Empty;
}

public sealed class ProductionPlanDetailsViewModel
{
    public int Id { get; set; }
    public string PlanCode { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal PlannedQuantity { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusTone { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string CreatedByUser { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class ProductionPlanFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(60)]
    [Display(Name = "Plan Code")]
    public string PlanCode { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Product")]
    public int ProductId { get; set; }

    [Required]
    [Range(0, 9999999)]
    [Display(Name = "Planned Quantity")]
    public decimal PlannedQuantity { get; set; }

    [Required]
    [Display(Name = "Start Date")]
    public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;

    [Required]
    [Display(Name = "End Date")]
    public DateTime EndDate { get; set; } = DateTime.UtcNow.Date.AddDays(7);

    [Required]
    public ProductionPlanStatus Status { get; set; } = ProductionPlanStatus.Draft;

    [StringLength(2000)]
    public string Notes { get; set; } = string.Empty;

    [Display(Name = "Created By")]
    public string CreatedByUser { get; set; } = string.Empty;

    public IReadOnlyList<ProductOptionViewModel> Products { get; set; } = Array.Empty<ProductOptionViewModel>();
}

public sealed class ProductOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SKU { get; set; }
}

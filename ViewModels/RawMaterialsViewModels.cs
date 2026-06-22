using System.ComponentModel.DataAnnotations;

namespace MeatPro.ViewModels;

public sealed class RawMaterialIndexViewModel
{
    public string Search { get; set; } = string.Empty;
    public string Sort { get; set; } = "newest";
    public IReadOnlyList<RawMaterialListItemViewModel> Items { get; set; } = Array.Empty<RawMaterialListItemViewModel>();
    public int TotalItems { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages { get; set; } = 1;
    public decimal TotalStock { get; set; }
    public decimal StockValue { get; set; }
    public int LowStockCount { get; set; }
}

public sealed class RawMaterialListItemViewModel
{
    public int Id { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal ReorderLevel { get; set; }
    public string Location { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusTone { get; set; } = string.Empty;
    public string ExpirationLabel { get; set; } = string.Empty;
}

public sealed class RawMaterialDetailsViewModel
{
    public int Id { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal UnitCost { get; set; }
    public string Location { get; set; } = string.Empty;
    public DateTime? ExpirationDate { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusTone { get; set; } = string.Empty;
    public decimal StockValue { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class RawMaterialFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(60)]
    public string SKU { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [Display(Name = "Unit of Measure")]
    public string UnitOfMeasure { get; set; } = "kg";

    [Range(0, 9999999)]
    [Display(Name = "Current Stock")]
    public decimal CurrentStock { get; set; }

    [Range(0, 9999999)]
    [Display(Name = "Reorder Level")]
    public decimal ReorderLevel { get; set; }

    [Range(0, 9999999)]
    [Display(Name = "Unit Cost")]
    public decimal UnitCost { get; set; }

    [Required]
    [StringLength(200)]
    public string Location { get; set; } = string.Empty;

    [Display(Name = "Expiration Date")]
    public DateTime? ExpirationDate { get; set; }

    [Required]
    [StringLength(200)]
    [Display(Name = "Supplier Name")]
    public string SupplierName { get; set; } = string.Empty;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}

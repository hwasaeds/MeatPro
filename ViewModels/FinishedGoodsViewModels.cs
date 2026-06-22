using System.ComponentModel.DataAnnotations;

namespace MeatPro.ViewModels;

public sealed class FinishedGoodIndexViewModel
{
    public string Search { get; set; } = string.Empty;
    public string Sort { get; set; } = "newest";
    public IReadOnlyList<FinishedGoodListItemViewModel> Items { get; set; } = Array.Empty<FinishedGoodListItemViewModel>();
    public int TotalItems { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages { get; set; } = 1;
    public decimal TotalUnits { get; set; }
    public decimal TotalValue { get; set; }
    public int ExpiringCount { get; set; }
}

public sealed class FinishedGoodListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal UnitPrice { get; set; }
    public string StorageLocation { get; set; } = string.Empty;
    public DateTime? ExpirationDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusTone { get; set; } = string.Empty;
}

public sealed class FinishedGoodDetailsViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal UnitPrice { get; set; }
    public string StorageLocation { get; set; } = string.Empty;
    public DateTime? ExpirationDate { get; set; }
    public bool IsActive { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusTone { get; set; } = string.Empty;
    public decimal StockValue { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class FinishedGoodFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(60)]
    public string SKU { get; set; } = string.Empty;

    [StringLength(60)]
    [Display(Name = "Batch Number")]
    public string BatchNumber { get; set; } = string.Empty;

    [Range(0, 9999999)]
    [Display(Name = "Current Stock")]
    public decimal CurrentStock { get; set; }

    [Range(0, 9999999)]
    [Display(Name = "Reorder Level")]
    public decimal ReorderLevel { get; set; }

    [Range(0, 9999999)]
    [Display(Name = "Unit Price")]
    public decimal UnitPrice { get; set; }

    [StringLength(200)]
    [Display(Name = "Storage Location")]
    public string StorageLocation { get; set; } = string.Empty;

    [Display(Name = "Expiration Date")]
    public DateTime? ExpirationDate { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}

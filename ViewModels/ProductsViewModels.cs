using System.ComponentModel.DataAnnotations;
using MeatPro.Models;

namespace MeatPro.ViewModels;

public sealed class ProductIndexViewModel
{
    public string Search { get; set; } = string.Empty;
    public string Sort { get; set; } = "newest";
    public IReadOnlyList<ProductListItemViewModel> Items { get; set; } = Array.Empty<ProductListItemViewModel>();
    public int TotalItems { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages { get; set; } = 1;
    public int ActiveCount { get; set; }
}

public sealed class ProductListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal SellingPrice { get; set; }
    public bool IsActive { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusTone { get; set; } = string.Empty;
}

public sealed class ProductDetailsViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal StandardYield { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal SellingPrice { get; set; }
    public bool IsActive { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusTone { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class ProductFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(60)]
    public string SKU { get; set; } = string.Empty;

    [Display(Name = "Category")]
    public int? ProductCategoryId { get; set; }

    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [Display(Name = "Unit of Measure")]
    public string UnitOfMeasure { get; set; } = "kg";

    [Range(0, 9999999)]
    [Display(Name = "Standard Yield")]
    public decimal StandardYield { get; set; }

    [Range(0, 9999999)]
    [Display(Name = "Reorder Level")]
    public decimal ReorderLevel { get; set; }

    [Range(0, 9999999)]
    [Display(Name = "Selling Price")]
    public decimal SellingPrice { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public IReadOnlyList<ProductCategoryOptionViewModel> Categories { get; set; } = Array.Empty<ProductCategoryOptionViewModel>();
}

public sealed class ProductCategoryOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

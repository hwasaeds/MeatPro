using System.ComponentModel.DataAnnotations;

namespace MeatPro.ViewModels;

public sealed class SupplierIndexViewModel
{
    public string Search { get; set; } = string.Empty;
    public string Sort { get; set; } = "newest";
    public IReadOnlyList<SupplierListItemViewModel> Items { get; set; } = Array.Empty<SupplierListItemViewModel>();
    public int TotalItems { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages { get; set; } = 1;
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
}

public sealed class SupplierListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusTone { get; set; } = string.Empty;
}

public sealed class SupplierDetailsViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusTone { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class SupplierFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Contact Person")]
    public string ContactPerson { get; set; } = string.Empty;

    [StringLength(60)]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [StringLength(500)]
    public string Address { get; set; } = string.Empty;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}
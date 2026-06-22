using System.ComponentModel.DataAnnotations;
using MeatPro.Models;

namespace MeatPro.ViewModels;

public sealed class PurchaseTransactionIndexViewModel
{
    public string Search { get; set; } = string.Empty;
    public string Sort { get; set; } = "newest";
    public string? StatusFilter { get; set; }
    public IReadOnlyList<PurchaseTransactionListItemViewModel> Items { get; set; } = Array.Empty<PurchaseTransactionListItemViewModel>();
    public int TotalItems { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages { get; set; } = 1;
    public decimal TotalSpend { get; set; }
    public int ReceivedCount { get; set; }
    public int PendingCount { get; set; }
}

public sealed class PurchaseTransactionListItemViewModel
{
    public int Id { get; set; }
    public string PurchaseNumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public DateTime PurchasedOn { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusTone { get; set; } = string.Empty;
}

public sealed class PurchaseTransactionDetailsViewModel
{
    public int Id { get; set; }
    public string PurchaseNumber { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public DateTime PurchasedOn { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusTone { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime? ReceivedOn { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class PurchaseTransactionFormViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(60)]
    [Display(Name = "Purchase Number")]
    public string PurchaseNumber { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Supplier")]
    public int SupplierId { get; set; }

    [Required]
    [Display(Name = "Purchase Date")]
    public DateTime PurchasedOn { get; set; } = DateTime.UtcNow;

    [Required]
    [Range(0, 9999999)]
    [Display(Name = "Total Amount")]
    public decimal TotalAmount { get; set; }

    [Required]
    public PurchaseStatus Status { get; set; } = PurchaseStatus.Draft;

    [Display(Name = "Received On")]
    public DateTime? ReceivedOn { get; set; }

    [StringLength(1000)]
    public string Notes { get; set; } = string.Empty;

    public IReadOnlyList<SupplierOptionViewModel> Suppliers { get; set; } = Array.Empty<SupplierOptionViewModel>();
}

public sealed class SupplierOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

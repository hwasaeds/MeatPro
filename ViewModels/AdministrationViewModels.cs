using System.ComponentModel.DataAnnotations;

namespace MeatPro.ViewModels;

public sealed class UserIndexViewModel
{
    public string Search { get; set; } = string.Empty;
    public string Sort { get; set; } = "username";
    public IReadOnlyList<UserListItemViewModel> Items { get; set; } = Array.Empty<UserListItemViewModel>();
    public int TotalItems { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages { get; set; } = 1;
    public int ActiveCount { get; set; }
    public int LockedCount { get; set; }
}

public sealed class UserListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Roles { get; set; } = string.Empty;
    public bool IsLockedOut { get; set; }
    public string Status => IsLockedOut ? "Locked" : "Active";
    public string StatusTone => IsLockedOut ? "warning" : "success";
}

public sealed class UserDetailsViewModel
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
    public bool IsLockedOut { get; set; }
    public string Status => IsLockedOut ? "Locked" : "Active";
    public string StatusTone => IsLockedOut ? "warning" : "success";
}

public sealed class UserFormViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required]
    [StringLength(256)]
    [Display(Name = "Username")]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [StringLength(200)]
    public string Department { get; set; } = string.Empty;

    [Phone]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }

    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8)]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public string? ConfirmPassword { get; set; }

    public IReadOnlyList<RoleOptionViewModel> AvailableRoles { get; set; } = Array.Empty<RoleOptionViewModel>();

    [Display(Name = "Locked Out")]
    public bool IsLockedOut { get; set; }

    public bool IsNew => string.IsNullOrWhiteSpace(Id);
}

public sealed class RoleOptionViewModel
{
    public string Name { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}

public sealed class RoleIndexViewModel
{
    public string Search { get; set; } = string.Empty;
    public IReadOnlyList<RoleListItemViewModel> Items { get; set; } = Array.Empty<RoleListItemViewModel>();
    public int TotalItems { get; set; }
}

public sealed class RoleListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int UserCount { get; set; }
}

public sealed class RoleDetailsViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public IReadOnlyList<string> Users { get; set; } = Array.Empty<string>();
    public int UserCount => Users.Count;
}

public sealed class RoleFormViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required]
    [StringLength(256)]
    [Display(Name = "Role Name")]
    public string Name { get; set; } = string.Empty;

    public bool IsNew => string.IsNullOrWhiteSpace(Id);
}

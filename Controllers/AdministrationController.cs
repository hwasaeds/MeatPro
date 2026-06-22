using MeatPro.Services;
using MeatPro.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeatPro.Controllers;

[Authorize]
public sealed class AdministrationController : Controller
{
    private readonly IAdministrationService _administrationService;

    public AdministrationController(IAdministrationService administrationService)
    {
        _administrationService = administrationService;
    }

    public async Task<IActionResult> Users(CancellationToken cancellationToken) => View("~/Views/Shared/ModuleIndex.cshtml", await _administrationService.BuildUsersPageAsync(cancellationToken));
    public async Task<IActionResult> Roles(CancellationToken cancellationToken) => View("~/Views/Shared/ModuleIndex.cshtml", await _administrationService.BuildRolesPageAsync(cancellationToken));
    public async Task<IActionResult> Settings(CancellationToken cancellationToken) => View("~/Views/Shared/ModuleIndex.cshtml", await _administrationService.BuildSettingsPageAsync(cancellationToken));

    public async Task<IActionResult> UserIndex(string? search, string? sort, int page = 1, CancellationToken cancellationToken = default)
    {
        var model = await _administrationService.BuildUserIndexAsync(search, sort, page, 10, cancellationToken);
        return View(model);
    }

    public async Task<IActionResult> UserDetails(string id, CancellationToken cancellationToken)
    {
        var model = await _administrationService.GetUserDetailsAsync(id, cancellationToken);
        if (model is null) return NotFound();
        return View(model);
    }

    public async Task<IActionResult> UserCreate(CancellationToken cancellationToken)
    {
        var model = new UserFormViewModel
        {
            AvailableRoles = await GetRoleOptionsAsync(cancellationToken)
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UserCreate(UserFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableRoles = await GetRoleOptionsAsync(cancellationToken);
            return View(model);
        }

        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), "Password is required for new users.");
            model.AvailableRoles = await GetRoleOptionsAsync(cancellationToken);
            return View(model);
        }

        if (await _administrationService.IsUserNameInUseAsync(model.UserName, cancellationToken: cancellationToken))
        {
            ModelState.AddModelError(nameof(model.UserName), "Username is already taken.");
            model.AvailableRoles = await GetRoleOptionsAsync(cancellationToken);
            return View(model);
        }

        if (await _administrationService.IsEmailInUseAsync(model.Email, cancellationToken: cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Email), "Email is already in use.");
            model.AvailableRoles = await GetRoleOptionsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            await _administrationService.CreateUserAsync(model, cancellationToken);
            TempData["SuccessMessage"] = "User created successfully.";
            return RedirectToAction(nameof(UserIndex));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model.AvailableRoles = await GetRoleOptionsAsync(cancellationToken);
            return View(model);
        }
    }

    public async Task<IActionResult> UserEdit(string id, CancellationToken cancellationToken)
    {
        var model = await _administrationService.GetUserEditAsync(id, cancellationToken);
        if (model is null) return NotFound();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UserEdit(string id, UserFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableRoles = await GetRoleOptionsAsync(cancellationToken);
            return View(model);
        }

        if (await _administrationService.IsUserNameInUseAsync(model.UserName, id, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.UserName), "Username is already taken.");
            model.AvailableRoles = await GetRoleOptionsAsync(cancellationToken);
            return View(model);
        }

        if (await _administrationService.IsEmailInUseAsync(model.Email, id, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Email), "Email is already in use.");
            model.AvailableRoles = await GetRoleOptionsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var success = await _administrationService.UpdateUserAsync(id, model, cancellationToken);
            if (!success) return NotFound();
            TempData["SuccessMessage"] = "User updated successfully.";
            return RedirectToAction(nameof(UserDetails), new { id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model.AvailableRoles = await GetRoleOptionsAsync(cancellationToken);
            return View(model);
        }
    }

    public async Task<IActionResult> UserDelete(string id, CancellationToken cancellationToken)
    {
        var model = await _administrationService.GetUserDeleteAsync(id, cancellationToken);
        if (model is null) return NotFound();
        return View(model);
    }

    [HttpPost]
    [ActionName("UserDelete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UserDeleteConfirmed(string id, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _administrationService.DeleteUserAsync(id, cancellationToken);
            if (!success) return NotFound();
            TempData["SuccessMessage"] = "User deleted successfully.";
            return RedirectToAction(nameof(UserIndex));
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(UserDelete), new { id });
        }
    }

    public async Task<IActionResult> RoleIndex(string? search, CancellationToken cancellationToken)
    {
        var model = await _administrationService.BuildRoleIndexAsync(search, cancellationToken);
        return View(model);
    }

    public async Task<IActionResult> RoleDetails(string id, CancellationToken cancellationToken)
    {
        var model = await _administrationService.GetRoleDetailsAsync(id, cancellationToken);
        if (model is null) return NotFound();
        return View(model);
    }

    public IActionResult RoleCreate()
    {
        return View(new RoleFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RoleCreate(RoleFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);

        if (await _administrationService.IsRoleNameInUseAsync(model.Name, cancellationToken: cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Name), "Role name is already taken.");
            return View(model);
        }

        try
        {
            await _administrationService.CreateRoleAsync(model, cancellationToken);
            TempData["SuccessMessage"] = "Role created successfully.";
            return RedirectToAction(nameof(RoleIndex));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    public async Task<IActionResult> RoleEdit(string id, CancellationToken cancellationToken)
    {
        var model = await _administrationService.GetRoleEditAsync(id, cancellationToken);
        if (model is null) return NotFound();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RoleEdit(string id, RoleFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);

        if (await _administrationService.IsRoleNameInUseAsync(model.Name, id, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Name), "Role name is already taken.");
            return View(model);
        }

        try
        {
            var success = await _administrationService.UpdateRoleAsync(id, model, cancellationToken);
            if (!success) return NotFound();
            TempData["SuccessMessage"] = "Role updated successfully.";
            return RedirectToAction(nameof(RoleDetails), new { id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    public async Task<IActionResult> RoleDelete(string id, CancellationToken cancellationToken)
    {
        var model = await _administrationService.GetRoleDeleteAsync(id, cancellationToken);
        if (model is null) return NotFound();
        return View(model);
    }

    [HttpPost]
    [ActionName("RoleDelete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RoleDeleteConfirmed(string id, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _administrationService.DeleteRoleAsync(id, cancellationToken);
            if (!success) return NotFound();
            TempData["SuccessMessage"] = "Role deleted successfully.";
            return RedirectToAction(nameof(RoleIndex));
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(RoleDelete), new { id });
        }
    }

    private async Task<IReadOnlyList<RoleOptionViewModel>> GetRoleOptionsAsync(CancellationToken cancellationToken)
    {
        var roles = await _administrationService.BuildRoleIndexAsync(null, cancellationToken);
        return roles.Items.Select(r => new RoleOptionViewModel
        {
            Name = r.Name,
            IsSelected = false
        }).ToList();
    }
}
using MeatPro.Data;
using MeatPro.Models;
using MeatPro.Services;
using MeatPro.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MeatPro.Controllers;

[Authorize]
public sealed class WorkOrdersController : Controller
{
    private readonly IWorkOrderService _workOrderService;
    private readonly ApplicationDbContext _context;

    public WorkOrdersController(IWorkOrderService workOrderService, ApplicationDbContext context)
    {
        _workOrderService = workOrderService;
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, string? sort, string? statusFilter, int page = 1, CancellationToken cancellationToken = default)
    {
        var model = await _workOrderService.BuildIndexAsync(search, sort, statusFilter, page, 10, cancellationToken);
        return View(model);
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
    {
        var model = await _workOrderService.GetDetailsAsync(id, cancellationToken);
        if (model is null) return NotFound();
        return View(model);
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
    {
        var plans = await GetProductionPlanOptionsAsync(cancellationToken);
        return View(new WorkOrderFormViewModel
        {
            ScheduledDate = DateTime.UtcNow.Date,
            Status = WorkOrderStatus.Draft,
            ProductionPlans = plans
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(WorkOrderFormViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            model.ProductionPlans = await GetProductionPlanOptionsAsync(cancellationToken);
            return View(model);
        }

        if (await _workOrderService.IsWorkOrderNumberInUseAsync(model.WorkOrderNumber, null, cancellationToken))
        {
            ModelState.AddModelError(nameof(WorkOrderFormViewModel.WorkOrderNumber), "That work order number is already in use.");
            model.ProductionPlans = await GetProductionPlanOptionsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var id = await _workOrderService.CreateAsync(model, cancellationToken);
            TempData["SuccessMessage"] = $"Work order {model.WorkOrderNumber} was created successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "The work order could not be saved. Please check the values and try again.");
            model.ProductionPlans = await GetProductionPlanOptionsAsync(cancellationToken);
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
    {
        var model = await _workOrderService.GetEditAsync(id, cancellationToken);
        if (model is null) return NotFound();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, WorkOrderFormViewModel model, CancellationToken cancellationToken = default)
    {
        if (id != model.Id) return NotFound();

        if (!ModelState.IsValid)
        {
            model.ProductionPlans = await GetProductionPlanOptionsAsync(cancellationToken);
            return View(model);
        }

        if (await _workOrderService.IsWorkOrderNumberInUseAsync(model.WorkOrderNumber, id, cancellationToken))
        {
            ModelState.AddModelError(nameof(WorkOrderFormViewModel.WorkOrderNumber), "That work order number is already in use.");
            model.ProductionPlans = await GetProductionPlanOptionsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var updated = await _workOrderService.UpdateAsync(id, model, cancellationToken);
            if (!updated) return NotFound();

            TempData["SuccessMessage"] = $"Work order {model.WorkOrderNumber} was updated successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "The work order could not be updated. Please check the values and try again.");
            model.ProductionPlans = await GetProductionPlanOptionsAsync(cancellationToken);
            return View(model);
        }
    }

    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var model = await _workOrderService.GetDeleteAsync(id, cancellationToken);
        if (model is null) return NotFound();
        return View(model);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken = default)
    {
        var deleted = await _workOrderService.DeleteAsync(id, cancellationToken);
        if (!deleted) return NotFound();

        TempData["SuccessMessage"] = "Work order deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, CancellationToken cancellationToken = default)
    {
        var (success, message) = await _workOrderService.ApproveAsync(id, User.Identity?.Name ?? "system", cancellationToken);
        if (!success) { TempData["ErrorMessage"] = message; return RedirectToAction(nameof(Details), new { id }); }
        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int id, CancellationToken cancellationToken = default)
    {
        var (success, message) = await _workOrderService.StartAsync(id, User.Identity?.Name ?? "system", cancellationToken);
        if (!success) { TempData["ErrorMessage"] = message; return RedirectToAction(nameof(Details), new { id }); }
        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Hold(int id, CancellationToken cancellationToken = default)
    {
        var (success, message) = await _workOrderService.HoldAsync(id, User.Identity?.Name ?? "system", cancellationToken);
        if (!success) { TempData["ErrorMessage"] = message; return RedirectToAction(nameof(Details), new { id }); }
        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resume(int id, CancellationToken cancellationToken = default)
    {
        var (success, message) = await _workOrderService.ResumeAsync(id, User.Identity?.Name ?? "system", cancellationToken);
        if (!success) { TempData["ErrorMessage"] = message; return RedirectToAction(nameof(Details), new { id }); }
        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Complete(int id, CancellationToken cancellationToken = default)
    {
        var details = await _workOrderService.GetDetailsAsync(id, cancellationToken);
        if (details is null) return NotFound();

        var plan = await _context.ProductionPlans.Include(x => x.Product).FirstOrDefaultAsync(x => x.Id == details.ProductionPlanId, cancellationToken);

        return View(new CompleteWorkOrderViewModel
        {
            ProducedQuantity = details.Quantity,
            ProducedAt = DateTime.UtcNow,
            BatchNumber = $"BAT-{details.WorkOrderNumber}-{DateTime.UtcNow:yyyyMMddHHmmss}"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id, CompleteWorkOrderViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return View(model);

        var (success, message) = await _workOrderService.CompleteAsync(id, model, User.Identity?.Name ?? "system", cancellationToken);
        if (!success) { ModelState.AddModelError(string.Empty, message); return View(model); }

        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken = default)
    {
        var (success, message) = await _workOrderService.CancelAsync(id, User.Identity?.Name ?? "system", cancellationToken);
        if (!success) { TempData["ErrorMessage"] = message; return RedirectToAction(nameof(Details), new { id }); }
        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<List<ProductionPlanOptionViewModel>> GetProductionPlanOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ProductionPlans.AsNoTracking().Include(x => x.Product).OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new ProductionPlanOptionViewModel { Id = x.Id, PlanCode = x.PlanCode, ProductName = x.Product!.Name })
            .ToListAsync(cancellationToken);
    }
}

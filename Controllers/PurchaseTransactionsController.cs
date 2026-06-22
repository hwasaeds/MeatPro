using System.Security.Claims;
using MeatPro.Data;
using MeatPro.Models;
using MeatPro.Services;
using MeatPro.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MeatPro.Controllers;

[Authorize]
public sealed class PurchaseTransactionsController : Controller
{
    private readonly IPurchaseTransactionService _purchaseTransactionService;
    private readonly IAuditTrailService _auditTrail;
    private readonly ApplicationDbContext _context;

    public PurchaseTransactionsController(IPurchaseTransactionService purchaseTransactionService, IAuditTrailService auditTrail, ApplicationDbContext context)
    {
        _purchaseTransactionService = purchaseTransactionService;
        _auditTrail = auditTrail;
        _context = context;
    }

    private string CurrentUser => User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email) ?? "unknown";

    public async Task<IActionResult> Index(string? search, string? sort, string? statusFilter, int page = 1, CancellationToken cancellationToken = default)
    {
        var model = await _purchaseTransactionService.BuildIndexAsync(search, sort, statusFilter, page, 10, cancellationToken);
        return View(model);
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
    {
        var model = await _purchaseTransactionService.GetDetailsAsync(id, cancellationToken);
        if (model is null) return NotFound();
        return View(model);
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
    {
        var suppliers = await GetSupplierOptionsAsync(cancellationToken);
        return View(new PurchaseTransactionFormViewModel
        {
            PurchasedOn = DateTime.UtcNow,
            Status = PurchaseStatus.Draft,
            Suppliers = suppliers
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PurchaseTransactionFormViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            model.Suppliers = await GetSupplierOptionsAsync(cancellationToken);
            return View(model);
        }

        if (await _purchaseTransactionService.IsPurchaseNumberInUseAsync(model.PurchaseNumber, null, cancellationToken))
        {
            ModelState.AddModelError(nameof(PurchaseTransactionFormViewModel.PurchaseNumber), "That purchase number is already in use.");
            model.Suppliers = await GetSupplierOptionsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var id = await _purchaseTransactionService.CreateAsync(model, cancellationToken);
            TempData["SuccessMessage"] = $"Purchase {model.PurchaseNumber} was created successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "The purchase could not be saved. Please check the values and try again.");
            model.Suppliers = await GetSupplierOptionsAsync(cancellationToken);
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
    {
        var model = await _purchaseTransactionService.GetEditAsync(id, cancellationToken);
        if (model is null) return NotFound();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PurchaseTransactionFormViewModel model, CancellationToken cancellationToken = default)
    {
        if (id != model.Id) return NotFound();

        if (!ModelState.IsValid)
        {
            model.Suppliers = await GetSupplierOptionsAsync(cancellationToken);
            return View(model);
        }

        if (await _purchaseTransactionService.IsPurchaseNumberInUseAsync(model.PurchaseNumber, id, cancellationToken))
        {
            ModelState.AddModelError(nameof(PurchaseTransactionFormViewModel.PurchaseNumber), "That purchase number is already in use.");
            model.Suppliers = await GetSupplierOptionsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var updated = await _purchaseTransactionService.UpdateAsync(id, model, cancellationToken);
            if (!updated) return NotFound();

            TempData["SuccessMessage"] = $"Purchase {model.PurchaseNumber} was updated successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "The purchase could not be updated. Please check the values and try again.");
            model.Suppliers = await GetSupplierOptionsAsync(cancellationToken);
            return View(model);
        }
    }

    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var model = await _purchaseTransactionService.GetDeleteAsync(id, cancellationToken);
        if (model is null) return NotFound();
        return View(model);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken = default)
    {
        var deleted = await _purchaseTransactionService.DeleteAsync(id, cancellationToken);
        if (!deleted) return NotFound();

        TempData["SuccessMessage"] = "Purchase transaction deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Receive(int id, CancellationToken cancellationToken = default)
    {
        var model = await _purchaseTransactionService.GetDetailsAsync(id, cancellationToken);
        if (model is null) return NotFound();

        return View(new ReceivePurchaseViewModel
        {
            PurchaseTransactionId = id,
            PurchaseNumber = model.PurchaseNumber,
            SupplierName = model.SupplierName,
            TotalAmount = model.TotalAmount,
            ReceivedOn = DateTime.UtcNow
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Receive(int id, ReceivePurchaseViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return View(model);

        var transaction = await _context.PurchaseTransactions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (transaction is null) return NotFound();

        if (transaction.Status == PurchaseStatus.Received)
        {
            ModelState.AddModelError(string.Empty, "This purchase has already been received.");
            return View(model);
        }

        var oldStatus = transaction.Status;
        transaction.Status = PurchaseStatus.Received;
        transaction.ReceivedOn = model.ReceivedOn;
        transaction.Notes = string.IsNullOrWhiteSpace(model.Notes) ? transaction.Notes : model.Notes;
        transaction.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        await _auditTrail.LogAsync("Procurement", "Receive", "PurchaseTransaction", id.ToString(), CurrentUser,
            new { Status = oldStatus.ToString() }, new { Status = PurchaseStatus.Received.ToString() }, cancellationToken: cancellationToken);

        TempData["SuccessMessage"] = $"Purchase {transaction.PurchaseNumber} has been marked as received.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<List<SupplierOptionViewModel>> GetSupplierOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Suppliers.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new SupplierOptionViewModel { Id = x.Id, Name = x.Name })
            .ToListAsync(cancellationToken);
    }
}

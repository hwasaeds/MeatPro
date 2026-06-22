using MeatPro.Data;
using MeatPro.Models;
using MeatPro.Services;
using MeatPro.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MeatPro.Controllers.Api;

[Route("api/PurchaseTransactions")]
public sealed class PurchaseTransactionsApiController : ApiController
{
    private readonly IPurchaseTransactionService _purchaseTransactionService;
    private readonly ApplicationDbContext _context;
    private readonly IAuditTrailService _auditTrail;

    public PurchaseTransactionsApiController(IPurchaseTransactionService purchaseTransactionService, ApplicationDbContext context, IAuditTrailService auditTrail)
    {
        _purchaseTransactionService = purchaseTransactionService;
        _context = context;
        _auditTrail = auditTrail;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(string? search, string? sort, string? statusFilter, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await _purchaseTransactionService.BuildIndexAsync(search, sort, statusFilter, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        return OkOrNotFound(await _purchaseTransactionService.GetDetailsAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PurchaseTransactionFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (await _purchaseTransactionService.IsPurchaseNumberInUseAsync(model.PurchaseNumber, cancellationToken: cancellationToken))
            return Error("Purchase number is already in use.");

        var id = await _purchaseTransactionService.CreateAsync(model, cancellationToken);
        return Created(id.ToString(), new { id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] PurchaseTransactionFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (await _purchaseTransactionService.IsPurchaseNumberInUseAsync(model.PurchaseNumber, id, cancellationToken))
            return Error("Purchase number is already in use.");

        var success = await _purchaseTransactionService.UpdateAsync(id, model, cancellationToken);
        return success ? Ok(new { id }) : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var success = await _purchaseTransactionService.DeleteAsync(id, cancellationToken);
        return success ? Ok(new { id }) : NotFound();
    }

    [HttpPost("{id}/receive")]
    public async Task<IActionResult> Receive(int id, [FromBody] ApiReceiveRequest request, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var transaction = await _context.PurchaseTransactions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (transaction is null) return NotFound(new { error = "Purchase transaction not found." });

        if (transaction.Status == PurchaseStatus.Received)
            return Error("This purchase has already been received.");

        var oldStatus = transaction.Status;
        transaction.Status = PurchaseStatus.Received;
        transaction.ReceivedOn = request.ReceivedOn;
        transaction.Notes = string.IsNullOrWhiteSpace(request.Notes) ? transaction.Notes : request.Notes;
        transaction.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditTrail.LogAsync("Procurement", "Receive", "PurchaseTransaction", id.ToString(), request.PerformedBy,
            new { Status = oldStatus.ToString() }, new { Status = PurchaseStatus.Received.ToString() }, cancellationToken: cancellationToken);

        return Ok(new { id, status = "Received" });
    }
}

public sealed class ApiReceiveRequest
{
    public DateTime ReceivedOn { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
}

using MeatPro.Services;
using MeatPro.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace MeatPro.Controllers.Api;

[Route("api/Suppliers")]
public sealed class SuppliersApiController : ApiController
{
    private readonly ISupplierService _supplierService;

    public SuppliersApiController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(string? search, string? sort, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await _supplierService.BuildIndexAsync(search, sort, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        return OkOrNotFound(await _supplierService.GetDetailsAsync(id, cancellationToken));
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        return Ok(await _supplierService.GetAllActiveAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SupplierFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (await _supplierService.IsNameInUseAsync(model.Name, cancellationToken: cancellationToken))
            return Error("Supplier name is already in use.");

        var id = await _supplierService.CreateAsync(model, cancellationToken);
        return Created(id.ToString(), new { id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] SupplierFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (await _supplierService.IsNameInUseAsync(model.Name, id, cancellationToken))
            return Error("Supplier name is already in use.");

        var success = await _supplierService.UpdateAsync(id, model, cancellationToken);
        return success ? Ok(new { id }) : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var success = await _supplierService.DeleteAsync(id, cancellationToken);
        return success ? Ok(new { id }) : NotFound();
    }
}

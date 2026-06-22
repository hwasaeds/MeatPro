using MeatPro.Services;
using MeatPro.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace MeatPro.Controllers.Api;

[Route("api/ProductionBatches")]
public sealed class ProductionBatchesApiController : ApiController
{
    private readonly IProductionBatchService _productionBatchService;

    public ProductionBatchesApiController(IProductionBatchService productionBatchService)
    {
        _productionBatchService = productionBatchService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(string? search, string? sort, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await _productionBatchService.BuildIndexAsync(search, sort, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        return OkOrNotFound(await _productionBatchService.GetDetailsAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProductionBatchFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (await _productionBatchService.IsBatchNumberInUseAsync(model.BatchNumber, cancellationToken: cancellationToken))
            return Error("Batch number is already in use.");

        var id = await _productionBatchService.CreateAsync(model, cancellationToken);
        return Created(id.ToString(), new { id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] ProductionBatchFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (await _productionBatchService.IsBatchNumberInUseAsync(model.BatchNumber, id, cancellationToken))
            return Error("Batch number is already in use.");

        var success = await _productionBatchService.UpdateAsync(id, model, cancellationToken);
        return success ? Ok(new { id }) : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var success = await _productionBatchService.DeleteAsync(id, cancellationToken);
        return success ? Ok(new { id }) : NotFound();
    }
}

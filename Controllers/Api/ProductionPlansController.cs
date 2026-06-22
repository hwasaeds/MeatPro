using MeatPro.Models;
using MeatPro.Services;
using MeatPro.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace MeatPro.Controllers.Api;

[Route("api/ProductionPlans")]
public sealed class ProductionPlansApiController : ApiController
{
    private readonly IProductionPlanService _productionPlanService;

    public ProductionPlansApiController(IProductionPlanService productionPlanService)
    {
        _productionPlanService = productionPlanService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(string? search, string? sort, string? statusFilter, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await _productionPlanService.BuildIndexAsync(search, sort, statusFilter, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        return OkOrNotFound(await _productionPlanService.GetDetailsAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProductionPlanFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (await _productionPlanService.IsPlanCodeInUseAsync(model.PlanCode, cancellationToken: cancellationToken))
            return Error("Plan code is already in use.");

        var id = await _productionPlanService.CreateAsync(model, cancellationToken);
        return Created(id.ToString(), new { id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] ProductionPlanFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (await _productionPlanService.IsPlanCodeInUseAsync(model.PlanCode, id, cancellationToken))
            return Error("Plan code is already in use.");

        var success = await _productionPlanService.UpdateAsync(id, model, cancellationToken);
        return success ? Ok(new { id }) : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var success = await _productionPlanService.DeleteAsync(id, cancellationToken);
        return success ? Ok(new { id }) : NotFound();
    }
}

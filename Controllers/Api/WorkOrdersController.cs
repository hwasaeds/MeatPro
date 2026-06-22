using MeatPro.Models;
using MeatPro.Services;
using MeatPro.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace MeatPro.Controllers.Api;

[Route("api/WorkOrders")]
public sealed class WorkOrdersApiController : ApiController
{
    private readonly IWorkOrderService _workOrderService;

    public WorkOrdersApiController(IWorkOrderService workOrderService)
    {
        _workOrderService = workOrderService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(string? search, string? sort, string? statusFilter, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await _workOrderService.BuildIndexAsync(search, sort, statusFilter, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        return OkOrNotFound(await _workOrderService.GetDetailsAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] WorkOrderFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (await _workOrderService.IsWorkOrderNumberInUseAsync(model.WorkOrderNumber, cancellationToken: cancellationToken))
            return Error("Work order number is already in use.");

        var id = await _workOrderService.CreateAsync(model, cancellationToken);
        return Created(id.ToString(), new { id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] WorkOrderFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (await _workOrderService.IsWorkOrderNumberInUseAsync(model.WorkOrderNumber, id, cancellationToken))
            return Error("Work order number is already in use.");

        var success = await _workOrderService.UpdateAsync(id, model, cancellationToken);
        return success ? Ok(new { id }) : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var success = await _workOrderService.DeleteAsync(id, cancellationToken);
        return success ? Ok(new { id }) : NotFound();
    }
}

using MeatPro.Services;
using MeatPro.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace MeatPro.Controllers.Api;

[Route("api/FinishedGoods")]
public sealed class FinishedGoodsApiController : ApiController
{
    private readonly IFinishedGoodService _finishedGoodService;
    private readonly IStockService _stockService;

    public FinishedGoodsApiController(IFinishedGoodService finishedGoodService, IStockService stockService)
    {
        _finishedGoodService = finishedGoodService;
        _stockService = stockService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(string? search, string? sort, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await _finishedGoodService.BuildIndexAsync(search, sort, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        return OkOrNotFound(await _finishedGoodService.GetDetailsAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] FinishedGoodFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (await _finishedGoodService.IsSkuInUseAsync(model.SKU, cancellationToken: cancellationToken))
            return Error("SKU is already in use.");

        var id = await _finishedGoodService.CreateAsync(model, cancellationToken);
        return Created(id.ToString(), new { id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] FinishedGoodFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (await _finishedGoodService.IsSkuInUseAsync(model.SKU, id, cancellationToken))
            return Error("SKU is already in use.");

        var success = await _finishedGoodService.UpdateAsync(id, model, cancellationToken);
        return success ? Ok(new { id }) : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var success = await _finishedGoodService.DeleteAsync(id, cancellationToken);
        return success ? Ok(new { id }) : NotFound();
    }

    [HttpPost("{id}/adjust")]
    public async Task<IActionResult> Adjust(int id, [FromBody] AdjustRequest request, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var (success, message) = await _stockService.AdjustFinishedGoodAsync(id, request.NewQuantity, request.Reason, request.PerformedBy, cancellationToken);
        return success ? Ok(new { id, message }) : Error(message);
    }
}

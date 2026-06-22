using MeatPro.Services;
using MeatPro.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace MeatPro.Controllers.Api;

[Route("api/RawMaterials")]
public sealed class RawMaterialsApiController : ApiController
{
    private readonly IRawMaterialService _rawMaterialService;

    public RawMaterialsApiController(IRawMaterialService rawMaterialService)
    {
        _rawMaterialService = rawMaterialService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(string? search, string? sort, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await _rawMaterialService.BuildIndexAsync(search, sort, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        return OkOrNotFound(await _rawMaterialService.GetDetailsAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RawMaterialFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (await _rawMaterialService.IsSkuInUseAsync(model.SKU, cancellationToken: cancellationToken))
            return Error("SKU is already in use.");

        var id = await _rawMaterialService.CreateAsync(model, cancellationToken);
        return Created(id.ToString(), new { id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] RawMaterialFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (await _rawMaterialService.IsSkuInUseAsync(model.SKU, id, cancellationToken))
            return Error("SKU is already in use.");

        var success = await _rawMaterialService.UpdateAsync(id, model, cancellationToken);
        return success ? Ok(new { id }) : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var success = await _rawMaterialService.DeleteAsync(id, cancellationToken);
        return success ? Ok(new { id }) : NotFound();
    }
}

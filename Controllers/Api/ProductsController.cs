using MeatPro.Services;
using MeatPro.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace MeatPro.Controllers.Api;

[Route("api/Products")]
public sealed class ProductsApiController : ApiController
{
    private readonly IProductService _productService;

    public ProductsApiController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(string? search, string? sort, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await _productService.BuildIndexAsync(search, sort, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        return OkOrNotFound(await _productService.GetDetailsAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProductFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (await _productService.IsSkuInUseAsync(model.SKU, cancellationToken: cancellationToken))
            return Error("SKU is already in use.");

        var id = await _productService.CreateAsync(model, cancellationToken);
        return Created(id.ToString(), new { id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] ProductFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (await _productService.IsSkuInUseAsync(model.SKU, id, cancellationToken))
            return Error("SKU is already in use.");

        var success = await _productService.UpdateAsync(id, model, cancellationToken);
        return success ? Ok(new { id }) : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var success = await _productService.DeleteAsync(id, cancellationToken);
        return success ? Ok(new { id }) : NotFound();
    }
}

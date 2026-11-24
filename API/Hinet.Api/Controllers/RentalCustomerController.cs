using Hinet.Api;
using Hinet.Api.Dto;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Hinet.Controllers;

[Route("api/[controller]")]
public sealed class RentalCustomerController(RentalCustomerService service) : HinetController
{
    [HttpPost("GetData")]
    public async Task<IActionResult> GetData(RentalCustomerSearch search, CancellationToken ct) => Ok(DataResponse.Success(await service.Search(search, ct)));

    [HttpGet("Get/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var customer = await service.Get(id, ct);
        return customer is null ? NotFound(DataResponse.False("Không tìm thấy khách thuê.")) : Ok(DataResponse.Success(customer));
    }

    [HttpPost("Create")]
    public Task<IActionResult> Create(RentalCustomerRequest input, CancellationToken ct) => Execute(async () => await service.Create(input, ct));

    [HttpPut("Update")]
    public Task<IActionResult> Update(RentalCustomerRequest input, CancellationToken ct) => Execute(async () => await service.Update(input, ct));

    [HttpDelete("Delete/{id:guid}")]
    public Task<IActionResult> Delete(Guid id, CancellationToken ct) => Execute(async () => { await service.Delete(id, UserId!.Value, ct); return null; });

    public sealed record ResetRequest([Required, MinLength(8), MaxLength(128)] string Password);
    [HttpPut("ResetPassword/{id:guid}")]
    public Task<IActionResult> ResetPassword(Guid id, ResetRequest input, CancellationToken ct) => Execute(async () => { await service.Reset(id, input.Password, ct); return null; });

    private async Task<IActionResult> Execute(Func<Task<object?>> action)
    {
        try { return Ok(DataResponse.Success(await action())); }
        catch (ArgumentException ex) { return BadRequest(DataResponse.False(ex.Message)); }
        catch (KeyNotFoundException ex) { return NotFound(DataResponse.False(ex.Message)); }
    }
}

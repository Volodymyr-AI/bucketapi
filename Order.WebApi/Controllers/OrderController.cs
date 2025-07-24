using Microsoft.AspNetCore.Mvc;
using Order.Database.DTO;
using Order.Infrastructure.DTO;

namespace Order.WebApi.Controllers;

[ApiVersion("1.0")]
[ApiVersionNeutral]
[Produces("application/json")]
[Route("api/v{version:apiVersion}/[controller]")]
public class OrderController : AppController
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Guid>> CreateOrder([FromBody] CreateCustomerOrderDTO orderDto)
    {
        var command = orderDto.ToCommand();
        
        var orderId = await Mediator.Send(command);
        return CreatedAtAction(nameof(CreateOrder), new { orderId = orderId }, orderId);
    }
}
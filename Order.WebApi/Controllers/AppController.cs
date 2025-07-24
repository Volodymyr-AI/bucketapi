using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Order.WebApi.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class AppController : ControllerBase
{
    private IMediator _mediator;
    protected IMediator Mediator => _mediator ??= HttpContext.RequestServices.GetService<IMediator>();
    
    //internal string UserId => !User.Identity.IsAuthenticated
    //    ? Guid.Empty.ToString()
    //    : Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value).ToString();
}
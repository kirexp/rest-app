using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using RestManager.Managers;
using RestManager.Models;

namespace RestManager.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RestaurantManagerController : ControllerBase
{
    private readonly IRestManagerService _restManagerService;

    public RestaurantManagerController(IRestManagerService restManagerService)
    {
        _restManagerService = restManagerService;
    }

    [HttpGet("{groupId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Table))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Lookup([FromRoute] int groupId)
    {
        var lookupResult = _restManagerService.Lookup(groupId);
        return lookupResult == null ? NotFound() : Ok(lookupResult);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Arrive(ClientsGroup group)
    {
        _restManagerService.OnArrive(group);
        return Ok();
    }

    // not sure about http-delete. 
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Leave(ClientsGroup group)
    {
        _restManagerService.OnLeave(group);
        return Ok();
    }
}
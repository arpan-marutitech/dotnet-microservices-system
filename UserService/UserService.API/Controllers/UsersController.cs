using Microsoft.AspNetCore.Mvc;
using UserService.Application.Common;
using UserService.Application.DTOs;
using UserService.Application.Services.Interfaces;

namespace UserService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public ActionResult<ApiResponse<List<UserDto>>> GetAll()
    {
        return Ok(_userService.GetAll());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetById(int id)
    {
        var response = await _userService.GetById(id);

        if (!response.Success || response.Data == null)
        {
            return NotFound(response);
        }

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> Create([FromBody] CreateUserDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<UserDto>.FailResponse("Invalid input"));

        return Ok(await _userService.Create(dto));
    }

    [HttpPost("internal/sync")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<ActionResult<ApiResponse<UserDto>>> SyncAuthUser([FromBody] SyncUserDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<UserDto>.FailResponse("Invalid input"));

        return Ok(await _userService.SyncAuthUser(dto));
    }

    [HttpPut("{id:int}")]
    public ActionResult<ApiResponse<UserDto>> Update(int id, [FromBody] UpdateUserDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<UserDto>.FailResponse("Invalid input"));

        return Ok(_userService.Update(id, dto));
    }

    [HttpDelete("{id:int}")]
    public ActionResult<ApiResponse<bool>> Delete(int id)
    {
        return Ok(_userService.Delete(id));
    }
}
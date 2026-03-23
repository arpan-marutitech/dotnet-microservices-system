using Microsoft.AspNetCore.Mvc;
using AuthService.Application.Common;
using AuthService.Application.DTOs;
using AuthService.Application.Services.Interfaces;

namespace AuthService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public ActionResult<ApiResponse<string>> Register([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<string>.FailResponse("Invalid input"));

        return Ok(_authService.Register(dto));
    }

    [HttpPost("login")]
    public ActionResult<ApiResponse<string>> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<string>.FailResponse("Invalid input"));

        return Ok(_authService.Login(dto));
    }
}
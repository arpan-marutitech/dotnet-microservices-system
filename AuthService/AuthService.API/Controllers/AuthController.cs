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

    [HttpGet("internal/by-email")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public ActionResult<ApiResponse<AuthUserDto>> GetByEmail([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(ApiResponse<AuthUserDto>.FailResponse("Email is required"));

        var user = _authService.GetByEmail(email);
        if (user == null)
            return NotFound(ApiResponse<AuthUserDto>.FailResponse("User not found"));

        return Ok(ApiResponse<AuthUserDto>.SuccessResponse(user, "User fetched successfully"));
    }

    [HttpPost("internal/register")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public ActionResult<ApiResponse<AuthUserDto>> RegisterInternal([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<AuthUserDto>.FailResponse("Invalid input"));

        var user = _authService.RegisterInternal(dto);
        return Ok(ApiResponse<AuthUserDto>.SuccessResponse(user, "User registered successfully"));
    }
}
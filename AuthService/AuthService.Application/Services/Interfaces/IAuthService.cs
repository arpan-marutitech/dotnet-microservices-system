using AuthService.Application.Common;
using AuthService.Application.DTOs;

namespace AuthService.Application.Services.Interfaces;

public interface IAuthService
{
    ApiResponse<string> Register(RegisterDto dto);
    ApiResponse<string> Login(LoginDto dto);
}
using AutoMapper;
using AuthService.Application.Common;
using AuthService.Application.DTOs;
using AuthService.Application.Services.Interfaces;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Security;

namespace AuthService.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly JwtHelper _jwtHelper;
    private readonly IMapper _mapper;

    public AuthService(IUserRepository repo, JwtHelper jwtHelper, IMapper mapper)
    {
        _userRepository = repo;
        _jwtHelper = jwtHelper;
        _mapper = mapper;
    }

    public ApiResponse<string> Register(RegisterDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        var email = dto.Email.Trim().ToLower();

        var existingUser = _userRepository.GetByEmail(email);

        if (existingUser != null)
            throw new Exception("User already exists"); 

        var user = _mapper.Map<User>(dto);

        user.Email = email;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        user.Role = "User";

        _userRepository.Add(user);

        return ApiResponse<string>.SuccessResponse(
            "User Registered Successfully",
            "Success"
        );
    }

    public ApiResponse<string> Login(LoginDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        var email = dto.Email.Trim().ToLower();

        var user = _userRepository.GetByEmail(email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new Exception("Invalid credentials");

        var token = _jwtHelper.GenerateToken(user);

        return ApiResponse<string>.SuccessResponse(
            token,
            "Login successful"
        );
    }
}
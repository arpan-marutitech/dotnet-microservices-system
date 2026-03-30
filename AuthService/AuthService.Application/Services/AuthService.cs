using AutoMapper;
using AuthService.Application.Clients;
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
    private readonly UserServiceSyncClient _userServiceSyncClient;

    public AuthService(
        IUserRepository repo,
        JwtHelper jwtHelper,
        IMapper mapper,
        UserServiceSyncClient userServiceSyncClient)
    {
        _userRepository = repo;
        _jwtHelper = jwtHelper;
        _mapper = mapper;
        _userServiceSyncClient = userServiceSyncClient;
    }

    public ApiResponse<string> Register(RegisterDto dto)
    {
        var createdUser = RegisterInternal(dto);

        _userServiceSyncClient.SyncUserAsync(new SyncUserDto
        {
            AuthUserId = createdUser.Id,
            Name = createdUser.Name,
            Email = createdUser.Email,
            Password = dto.Password,
            Role = createdUser.Role
        }).GetAwaiter().GetResult();

        return ApiResponse<string>.SuccessResponse(
            "User Registered Successfully",
            "Success"
        );
    }

    public AuthUserDto RegisterInternal(RegisterDto dto)
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

        return ToAuthUserDto(user);
    }

    public AuthUserDto? GetByEmail(string email)
    {
        var normalizedEmail = email.Trim().ToLower();
        var user = _userRepository.GetByEmail(normalizedEmail);
        return user == null ? null : ToAuthUserDto(user);
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

    private static AuthUserDto ToAuthUserDto(User user)
    {
        return new AuthUserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role
        };
    }
}
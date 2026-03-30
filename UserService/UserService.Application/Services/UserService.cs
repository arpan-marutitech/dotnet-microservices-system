using AutoMapper;
using BCrypt.Net;
using UserService.Application.Clients;
using UserService.Application.Common;
using UserService.Application.DTOs;
using UserService.Application.Services.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;

namespace UserService.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _repository;
    private readonly IMapper _mapper;
    private readonly RedisService _redis;
    private readonly AuthServiceClient _authServiceClient;

    public UserService(
        IUserRepository repository,
        IMapper mapper,
        RedisService redis,
        AuthServiceClient authServiceClient)
    {
        _repository = repository;
        _mapper = mapper;
        _redis = redis;
        _authServiceClient = authServiceClient;
    }

    public ApiResponse<List<UserDto>> GetAll()
    {
        var users = _repository.GetAll();
        return ApiResponse<List<UserDto>>.SuccessResponse(_mapper.Map<List<UserDto>>(users), "Users fetched successfully");
    }

    public async Task<ApiResponse<UserDto>> GetById(int id)
    {
        var cacheKey = $"user:{id}";

        var cachedUser = await _redis.GetAsync<UserDto>(cacheKey);
        if (cachedUser != null)
        {
            Console.WriteLine("✅ From Cache");
            return ApiResponse<UserDto>.SuccessResponse(cachedUser, "User fetched successfully (cache)");
        }

        var user = _repository.GetById(id);
        if (user == null)
            return ApiResponse<UserDto>.FailResponse("User not found");

        var result = _mapper.Map<UserDto>(user);
        await _redis.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

        return ApiResponse<UserDto>.SuccessResponse(result, "User fetched successfully");
    }

    public async Task<ApiResponse<UserDto>> Create(CreateUserDto dto)
    {
        var email = dto.Email.Trim().ToLower();
        var authUser = await _authServiceClient.GetByEmailAsync(email)
            ?? await _authServiceClient.RegisterInternalAsync(dto);

        return await CreateOrSyncUserAsync(new SyncUserDto
        {
            AuthUserId = authUser.Id,
            Name = dto.Name,
            Email = email,
            Password = dto.Password,
            Role = string.IsNullOrWhiteSpace(dto.Role) ? authUser.Role : dto.Role
        }, "User created successfully");
    }

    public Task<ApiResponse<UserDto>> SyncAuthUser(SyncUserDto dto)
    {
        return CreateOrSyncUserAsync(dto, "User synced successfully");
    }

    private async Task<ApiResponse<UserDto>> CreateOrSyncUserAsync(SyncUserDto dto, string successMessage)
    {
        var email = dto.Email.Trim().ToLower();
        var existingById = _repository.GetById(dto.AuthUserId);
        if (existingById != null)
        {
            existingById.Name = dto.Name;
            existingById.Email = email;
            existingById.Role = string.IsNullOrWhiteSpace(dto.Role) ? existingById.Role : dto.Role;

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                existingById.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            }

            _repository.Update(existingById);
            await _redis.RemoveAsync($"user:{existingById.Id}");

            return ApiResponse<UserDto>.SuccessResponse(_mapper.Map<UserDto>(existingById), successMessage);
        }

        var existingByEmail = _repository.GetByEmail(email);
        if (existingByEmail != null && existingByEmail.Id != dto.AuthUserId)
        {
            _repository.Delete(existingByEmail.Id);
            await _redis.RemoveAsync($"user:{existingByEmail.Id}");
        }

        var user = new User
        {
            Id = dto.AuthUserId,
            Name = dto.Name,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = string.IsNullOrWhiteSpace(dto.Role) ? "User" : dto.Role
        };

        var created = _repository.Add(user, preserveIdentity: true);

        await _redis.RemoveAsync($"user:{created.Id}");

        return ApiResponse<UserDto>.SuccessResponse(_mapper.Map<UserDto>(created), successMessage);
    }

    public ApiResponse<UserDto> Update(int id, UpdateUserDto dto)
    {
        var existing = _repository.GetById(id);
        if (existing == null)
            return ApiResponse<UserDto>.FailResponse("User not found");

        existing.Name = dto.Name;
        existing.Email = dto.Email.Trim().ToLower();
        existing.Role = dto.Role;

        _repository.Update(existing);

        _redis.RemoveAsync($"user:{id}").GetAwaiter().GetResult();

        return ApiResponse<UserDto>.SuccessResponse(_mapper.Map<UserDto>(existing), "User updated successfully");
    }

    public ApiResponse<bool> Delete(int id)
    {
        var existing = _repository.GetById(id);
        if (existing == null)
            return ApiResponse<bool>.FailResponse("User not found");

        _repository.Delete(id);

        _redis.RemoveAsync($"user:{id}").GetAwaiter().GetResult();

        return ApiResponse<bool>.SuccessResponse(true, "User deleted successfully");
    }
}
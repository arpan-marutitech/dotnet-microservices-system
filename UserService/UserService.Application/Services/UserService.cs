using AutoMapper;
using BCrypt.Net;
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

    public UserService(IUserRepository repository, IMapper mapper, RedisService redis)
    {
        _repository = repository;
        _mapper = mapper;
        _redis = redis;
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

    public ApiResponse<UserDto> Create(CreateUserDto dto)
    {
        var existing = _repository.GetByEmail(dto.Email.Trim().ToLower());
        if (existing != null)
            return ApiResponse<UserDto>.FailResponse("User already exists");

        var user = _mapper.Map<User>(dto);
        user.Email = dto.Email.Trim().ToLower();
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var created = _repository.Add(user);

        _redis.RemoveAsync($"user:{created.Id}").GetAwaiter().GetResult();

        return ApiResponse<UserDto>.SuccessResponse(_mapper.Map<UserDto>(created), "User created successfully");
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
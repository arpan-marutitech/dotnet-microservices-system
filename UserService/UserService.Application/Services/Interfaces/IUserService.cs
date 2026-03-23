using UserService.Application.Common;
using UserService.Application.DTOs;

namespace UserService.Application.Services.Interfaces;

public interface IUserService
{
    ApiResponse<List<UserDto>> GetAll();
    Task<ApiResponse<UserDto>> GetById(int id);
    ApiResponse<UserDto> Create(CreateUserDto dto);
    ApiResponse<UserDto> Update(int id, UpdateUserDto dto);
    ApiResponse<bool> Delete(int id);
}
using System.ComponentModel.DataAnnotations;

namespace UserService.Application.DTOs;

public class UpdateUserDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}
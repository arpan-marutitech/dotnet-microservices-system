using System.Net;
using System.Net.Http.Json;
using UserService.Application.Common;
using UserService.Application.DTOs;

namespace UserService.Application.Clients;

public class AuthServiceClient
{
    private readonly HttpClient _httpClient;

    public AuthServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AuthUserDto?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/auth/internal/by-email?email={Uri.EscapeDataString(email)}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<AuthUserDto>>(cancellationToken: cancellationToken);
        return payload?.Data;
    }

    public async Task<AuthUserDto> RegisterInternalAsync(CreateUserDto dto, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/internal/register", new
        {
            dto.Name,
            Email = dto.Email.Trim().ToLower(),
            dto.Password
        }, cancellationToken);

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<AuthUserDto>>(cancellationToken: cancellationToken);
        if (payload is null || !payload.Success || payload.Data is null)
        {
            throw new InvalidOperationException(payload?.Message ?? "Failed to create auth user.");
        }

        return payload.Data;
    }
}
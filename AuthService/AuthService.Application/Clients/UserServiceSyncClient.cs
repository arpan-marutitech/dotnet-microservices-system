using System.Net.Http.Json;
using AuthService.Application.Common;
using AuthService.Application.DTOs;

namespace AuthService.Application.Clients;

public class UserServiceSyncClient
{
    private readonly HttpClient _httpClient;

    public UserServiceSyncClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task SyncUserAsync(SyncUserDto dto, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/users/internal/sync", dto, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(cancellationToken: cancellationToken);
        if (payload is null || !payload.Success)
        {
            throw new InvalidOperationException(payload?.Message ?? "Failed to sync user in UserService.");
        }
    }
}
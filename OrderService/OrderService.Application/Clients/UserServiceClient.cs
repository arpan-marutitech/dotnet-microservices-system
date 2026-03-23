using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace OrderService.Application.Clients;

public class UserServiceClient : IUserServiceClient
{
    private readonly HttpClient _httpClient;

    public UserServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> UserExists(int userId)
    {
        var response = await _httpClient.GetAsync($"api/users/{userId}");

        return response.StatusCode == HttpStatusCode.OK;
    }
}

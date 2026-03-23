using System.Threading.Tasks;

namespace OrderService.Application.Clients;

public interface IUserServiceClient
{
    Task<bool> UserExists(int userId);
}


using AuthService.Domain.Entities;

namespace AuthService.Domain.Interfaces;

public interface IUserRepository
{
    User? GetByEmail(string email);
    User? GetById(int id); 
    void Add(User user);
}
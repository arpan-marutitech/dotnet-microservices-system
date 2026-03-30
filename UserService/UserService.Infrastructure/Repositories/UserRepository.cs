using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;
using UserService.Infrastructure.Data;

namespace UserService.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public List<User> GetAll()
    {
        return _context.Users.AsNoTracking().ToList();
    }

    public User? GetById(int id)
    {
        return _context.Users.AsNoTracking().FirstOrDefault(u => u.Id == id);
    }

    public User? GetByEmail(string email)
    {
        return _context.Users.AsNoTracking().FirstOrDefault(u => u.Email == email);
    }

    public User Add(User user, bool preserveIdentity = false)
    {
        if (!preserveIdentity)
        {
            _context.Users.Add(user);
            _context.SaveChanges();
            return user;
        }

        var entityType = _context.Model.FindEntityType(typeof(User))
            ?? throw new InvalidOperationException("User entity metadata not found.");
        var schema = entityType.GetSchema() ?? "dbo";
        var tableName = entityType.GetTableName() ?? "Users";
        var fullyQualifiedTableName = $"[{schema}].[{tableName}]";

        var executionStrategy = _context.Database.CreateExecutionStrategy();
        return executionStrategy.Execute(() =>
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
#pragma warning disable EF1002
                _context.Database.ExecuteSqlRaw($"SET IDENTITY_INSERT {fullyQualifiedTableName} ON");
                _context.Users.Add(user);
                _context.SaveChanges();
                _context.Database.ExecuteSqlRaw($"SET IDENTITY_INSERT {fullyQualifiedTableName} OFF");
#pragma warning restore EF1002
                transaction.Commit();
                return user;
            }
            catch
            {
                try
                {
#pragma warning disable EF1002
                    _context.Database.ExecuteSqlRaw($"SET IDENTITY_INSERT {fullyQualifiedTableName} OFF");
#pragma warning restore EF1002
                }
                catch
                {
                }

                transaction.Rollback();
                throw;
            }
        });
    }

    public void Update(User user)
    {
        _context.Users.Update(user);
        _context.SaveChanges();
    }

    public void Delete(int id)
    {
        var user = _context.Users.Find(id);
        if (user == null) return;

        _context.Users.Remove(user);
        _context.SaveChanges();
    }
}
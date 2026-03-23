
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;
using OrderService.Infrastructure.Data;

namespace OrderService.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public List<Order> GetAll()
    {
        return _context.Orders.ToList();
    }

    public List<Order> GetByUserId(int userId)
    {
        return _context.Orders
            .Where(o => o.UserId == userId)
            .ToList();
    }

    public Order Add(Order order)
    {
        _context.Orders.Add(order);
        _context.SaveChanges();
        return order;
    }
}
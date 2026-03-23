using OrderService.Domain.Entities;
using System.Collections.Generic;

namespace OrderService.Domain.Interfaces;

public interface IOrderRepository
{
    List<Order> GetAll();
    List<Order> GetByUserId(int userId);
    Order Add(Order order);
}
using System.Threading.Tasks;
using OrderService.Application.DTOs;

using OrderService.Application.DTOs;

namespace OrderService.Application.Services.Interfaces;

public interface IOrderService
{
    Task<List<OrderResponseDto>> GetOrders(int userId);
    Task<OrderResponseDto> Create(OrderDto dto, int userId);
}
using AutoMapper;
using OrderService.Application.Clients;
using OrderService.Application.Common;
using OrderService.Application.DTOs;
using OrderService.Application.Messaging;
using OrderService.Application.Services.Interfaces;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMapper _mapper;
    private readonly IUserServiceClient _userClient;
    private readonly RabbitMqPublisher _publisher;
    private readonly RedisService _redis;

    public OrderService(
        IOrderRepository orderRepository,
        IMapper mapper,
        IUserServiceClient userClient,
        RabbitMqPublisher publisher,
        RedisService redis)
    {
        _orderRepository = orderRepository;
        _mapper = mapper;
        _userClient = userClient;
        _publisher = publisher;
        _redis = redis;
    }

    public async Task<List<OrderResponseDto>> GetOrders(int userId)
    {
        var cacheKey = $"orders:{userId}";

        var cachedOrders = await _redis.GetAsync<List<OrderResponseDto>>(cacheKey);
        if (cachedOrders != null)
        {
            Console.WriteLine("✅ Orders from Cache");
            return cachedOrders;
        }

        var orders = _orderRepository.GetByUserId(userId);
        var result = _mapper.Map<List<OrderResponseDto>>(orders);

        await _redis.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

        return result;
    }

    public async Task<OrderResponseDto> Create(OrderDto dto, int userId)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        var userExists = await _userClient.UserExists(userId);

		System.Console.WriteLine($"Checking if user exists for user ID: {userId} - Result: {userExists}");

        if (!userExists)
            throw new Exception("User does not exist");

        var order = _mapper.Map<Order>(dto);
        order.UserId = userId;

        var created = _orderRepository.Add(order);

        // Publish an event so other services can react (e.g., send notifications)
        await _publisher.PublishAsync(new OrderCreatedEvent
        {
            OrderId = created.Id,
            UserId = created.UserId,
            ProductName = created.ProductName
        });

        // Invalidate cached orders for this user
        await _redis.RemoveAsync($"orders:{userId}");

        return _mapper.Map<OrderResponseDto>(created);
    }
}
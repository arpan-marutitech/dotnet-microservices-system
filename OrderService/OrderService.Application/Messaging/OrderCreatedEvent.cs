namespace OrderService.Application.Messaging;

public class OrderCreatedEvent
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public string ProductName { get; set; } = default!;
}

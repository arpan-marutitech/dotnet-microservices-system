using Contracts;
using MassTransit;

namespace NotificationService.Services;

public class OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger) : IConsumer<OrderCreatedEvent>
{
    public Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var orderEvent = context.Message;

        logger.LogInformation("Order received: {ProductName}", orderEvent.ProductName);
        logger.LogInformation("Sending email to User {UserId}", orderEvent.UserId);

        return Task.CompletedTask;
    }
}
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace OrderService.Application.Messaging;

public class RabbitMqPublisher
{
    public async Task PublishAsync(OrderCreatedEvent eventMessage)
    {
        var factory = new ConnectionFactory()
        {
            HostName = "rabbitmq"
        };

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync(new CreateChannelOptions(false, false, null, null), CancellationToken.None);

        await channel.QueueDeclareAsync(
            queue: "order_created",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            passive: false,
            noWait: false,
            cancellationToken: CancellationToken.None
        );

        var message = JsonSerializer.Serialize(eventMessage);
        var body = Encoding.UTF8.GetBytes(message);

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: "order_created",
            body: body,
            cancellationToken: CancellationToken.None
        );
    }
}

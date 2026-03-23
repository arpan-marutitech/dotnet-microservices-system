using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NotificationService.Services;

public class RabbitMqConsumer : BackgroundService
{
    private readonly ILogger<RabbitMqConsumer> _logger;

    public RabbitMqConsumer(ILogger<RabbitMqConsumer> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory()
        {
            HostName = "rabbitmq",
            UserName = "guest",
            Password = "guest"
        };

        IConnection? connection = null;
        int retryCount = 5;

        while (retryCount > 0 && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                connection = await factory.CreateConnectionAsync(stoppingToken);
                break;
            }
            catch (Exception ex)
            {
                retryCount--;
                _logger.LogWarning(ex, "RabbitMQ not ready yet. Retrying in 5 seconds... ({RetryCount} retries left)", retryCount);

                if (retryCount == 0)
                {
                    break;
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Graceful cancellation during shutdown
                    return;
                }
            }
        }

        if (connection == null || !connection.IsOpen)
        {
            throw new InvalidOperationException("Could not connect to RabbitMQ after retry attempts.");
        }

        await using var channel = await connection.CreateChannelAsync(new CreateChannelOptions(false, false, null, null), stoppingToken);

        await channel.QueueDeclareAsync(
            queue: "order_created",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            passive: false,
            noWait: false,
            cancellationToken: stoppingToken
        );

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            var orderEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(message);

            if (orderEvent is null)
            {
                _logger.LogWarning("⚠️ Received malformed order event");
                return;
            }

            _logger.LogInformation("📩 Order Received: {ProductName}", orderEvent.ProductName);
            _logger.LogInformation("📧 Sending email to User {UserId}", orderEvent.UserId);

            await Task.CompletedTask;
        };

        await channel.BasicConsumeAsync(
            queue: "order_created",
            autoAck: true,
            consumer: consumer,
            cancellationToken: stoppingToken
        );

        // Keep the background service alive until cancellation.
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}

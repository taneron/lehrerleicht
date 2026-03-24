using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Lehrerleicht.Approval.Infrastructure.Messaging.Messages;

namespace Lehrerleicht.Approval.Infrastructure.Messaging;

public class RabbitMqConsumer : IAsyncDisposable
{
    private readonly ILogger<RabbitMqConsumer> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IChannel? _channel;

    private const string ExchangeName = "lehrerleicht.approvals";
    private const string QueueName = "pending.approvals";
    private const string RoutingKey = "approval.pending";

    public event Func<PendingApprovalMessage, Task>? OnMessageReceived;

    public RabbitMqConsumer(ILogger<RabbitMqConsumer> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
            Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = _configuration["RabbitMQ:Username"] ?? "guest",
            Password = _configuration["RabbitMQ:Password"] ?? "guest"
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await _channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await _channel.QueueBindAsync(
            queue: QueueName,
            exchange: ExchangeName,
            routingKey: RoutingKey,
            cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var message = JsonSerializer.Deserialize<PendingApprovalMessage>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (message is not null && OnMessageReceived is not null)
                {
                    await OnMessageReceived(message);
                }

                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);

        _logger.LogInformation("RabbitMQ consumer started on queue {Queue}", QueueName);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null) await _channel.CloseAsync();
        if (_connection is not null) await _connection.CloseAsync();
    }
}

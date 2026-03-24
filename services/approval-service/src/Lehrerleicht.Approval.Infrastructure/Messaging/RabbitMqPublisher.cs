using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Lehrerleicht.Approval.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Lehrerleicht.Approval.Infrastructure.Messaging;

public class RabbitMqPublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IChannel? _channel;
    private bool _initialized;

    private const string ExchangeName = "lehrerleicht.approvals";

    public RabbitMqPublisher(ILogger<RabbitMqPublisher> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initialized) return;

        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
            Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = _configuration["RabbitMQ:Username"] ?? "guest",
            Password = _configuration["RabbitMQ:Password"] ?? "guest"
        };

        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        await _channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Topic,
            durable: true);

        _initialized = true;
        _logger.LogInformation("RabbitMQ publisher initialized");
    }

    public async Task PublishApprovalResultAsync(string routingKey, object message)
    {
        await EnsureInitializedAsync();

        var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var body = Encoding.UTF8.GetBytes(json);

        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };

        await _channel!.BasicPublishAsync(
            exchange: ExchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body);

        _logger.LogInformation("Published message to {RoutingKey}", routingKey);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null) await _channel.CloseAsync();
        if (_connection is not null) await _connection.CloseAsync();
    }
}

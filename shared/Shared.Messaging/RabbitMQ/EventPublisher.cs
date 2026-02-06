using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Shared.Messaging.RabbitMQ;

public interface IEventPublisher
{
    void Publish<T>(string exchange, string routingKey, T message) where T : class;
    Task PublishAsync<T>(string exchange, string routingKey, T message) where T : class;
}

public class EventPublisher : IEventPublisher
{
    private readonly IRabbitMQConnection _connection;
    private readonly ILogger<EventPublisher> _logger;

    public EventPublisher(IRabbitMQConnection connection, ILogger<EventPublisher> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public void Publish<T>(string exchange, string routingKey, T message) where T : class
    {
        try
        {
            using var channel = _connection.CreateChannel();
            
            // Declare exchange (idempotent)
            channel.ExchangeDeclare(exchange, ExchangeType.Topic, durable: true);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            channel.BasicPublish(exchange, routingKey, properties, body);
            
            _logger.LogInformation("📤 Published {EventType} to {Exchange}/{RoutingKey}", 
                typeof(T).Name, exchange, routingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to publish {EventType} to {Exchange}/{RoutingKey}", 
                typeof(T).Name, exchange, routingKey);
            throw;
        }
    }

    public Task PublishAsync<T>(string exchange, string routingKey, T message) where T : class
    {
        return Task.Run(() => Publish(exchange, routingKey, message));
    }
}


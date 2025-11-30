using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Shared.Messaging.RabbitMQ;

public abstract class EventConsumer<T> : BackgroundService where T : class
{
    private readonly IRabbitMQConnection _connection;
    private readonly ILogger _logger;
    private readonly string _exchange;
    private readonly string _queue;
    private readonly string _routingKey;
    private IModel? _channel;

    protected EventConsumer(
        IRabbitMQConnection connection,
        ILogger logger,
        string exchange,
        string queue,
        string routingKey)
    {
        _connection = connection;
        _logger = logger;
        _exchange = exchange;
        _queue = queue;
        _routingKey = routingKey;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        try
        {
            _channel = _connection.CreateChannel();

            // Declare exchange
            _channel.ExchangeDeclare(_exchange, ExchangeType.Topic, durable: true);

            // Declare queue
            _channel.QueueDeclare(_queue, durable: true, exclusive: false, autoDelete: false);

            // Bind queue to exchange
            _channel.QueueBind(_queue, _exchange, _routingKey);

            // Set prefetch count for fair dispatch
            _channel.BasicQos(0, 10, false);

            var consumer = new EventingBasicConsumer(_channel);
            
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    var message = JsonSerializer.Deserialize<T>(json);

                    if (message != null)
                    {
                        _logger.LogInformation("📥 Received {EventType} from {Queue}", typeof(T).Name, _queue);
                        await HandleAsync(message, stoppingToken);
                        _channel.BasicAck(ea.DeliveryTag, false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error processing message from {Queue}", _queue);
                    // Requeue on failure (could implement dead letter queue)
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            _channel.BasicConsume(_queue, autoAck: false, consumer);
            _logger.LogInformation("🎧 Started consuming from {Queue}", _queue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to start consumer for {Queue}", _queue);
        }

        return Task.CompletedTask;
    }

    protected abstract Task HandleAsync(T message, CancellationToken cancellationToken);

    public override void Dispose()
    {
        _channel?.Dispose();
        base.Dispose();
    }
}


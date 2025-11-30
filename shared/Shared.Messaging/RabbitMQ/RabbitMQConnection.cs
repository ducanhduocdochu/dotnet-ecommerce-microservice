using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Shared.Messaging.RabbitMQ;

public interface IRabbitMQConnection : IDisposable
{
    bool IsConnected { get; }
    IModel CreateChannel();
    bool TryConnect();
}

public class RabbitMQConnection : IRabbitMQConnection
{
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<RabbitMQConnection> _logger;
    private IConnection? _connection;
    private readonly object _lock = new();
    private bool _disposed;

    public RabbitMQConnection(IOptions<RabbitMQSettings> settings, ILogger<RabbitMQConnection> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public bool IsConnected => _connection is { IsOpen: true } && !_disposed;

    public IModel CreateChannel()
    {
        if (!IsConnected)
        {
            TryConnect();
        }

        if (!IsConnected)
        {
            throw new InvalidOperationException("No RabbitMQ connection available");
        }

        return _connection!.CreateModel();
    }

    public bool TryConnect()
    {
        lock (_lock)
        {
            if (IsConnected) return true;

            var factory = new ConnectionFactory
            {
                HostName = _settings.HostName,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password,
                VirtualHost = _settings.VirtualHost,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            for (int i = 0; i < _settings.RetryCount; i++)
            {
                try
                {
                    _connection = factory.CreateConnection();
                    _logger.LogInformation("✅ RabbitMQ connection established to {Host}:{Port}", 
                        _settings.HostName, _settings.Port);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ RabbitMQ connection attempt {Attempt} failed", i + 1);
                    if (i < _settings.RetryCount - 1)
                    {
                        Thread.Sleep(_settings.RetryDelayMs);
                    }
                }
            }

            _logger.LogError("❌ Could not connect to RabbitMQ after {RetryCount} attempts", _settings.RetryCount);
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _connection?.Dispose();
    }
}


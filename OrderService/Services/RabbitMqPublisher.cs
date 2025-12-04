using OrderService.Interfaces;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace OrderService.Services
{
    public class RabbitMqPublisher : IRabbitMqPublisher, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<RabbitMqPublisher> _logger;
        private const string ExchangeName = "order_exchange";
        private const string RoutingKey = "order.created";

        public RabbitMqPublisher(IConfiguration config, ILogger<RabbitMqPublisher> logger)
        {
            _logger = logger;

            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = config["RabbitMQ:Host"] ?? "localhost",
                    UserName = config["RabbitMQ:Username"] ?? "guest",
                    Password = config["RabbitMQ:Password"] ?? "guest",
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
                _channel.ExchangeDeclare(ExchangeName, ExchangeType.Direct, durable: true);

                _logger.LogInformation("RabbitMQ Publisher connected successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to RabbitMQ");
                throw;
            }
        }

        public void PublishOrderCreated(object evt)
        {
            try
            {
                var json = JsonSerializer.Serialize(evt);
                var body = Encoding.UTF8.GetBytes(json);
                var props = _channel.CreateBasicProperties();
                props.Persistent = true;
                props.ContentType = "application/json";
                props.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                _channel.BasicPublish(ExchangeName, RoutingKey, props, body);

                _logger.LogInformation("Published OrderCreatedEvent to exchange {Exchange} with routing key {RoutingKey}: {Message}",
                    ExchangeName, RoutingKey, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish OrderCreatedEvent");
                throw;
            }
        }

        public void Dispose()
        {
            try
            {
                _channel?.Close();
                _connection?.Close();
                _logger.LogInformation("RabbitMQ Publisher disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing RabbitMQ Publisher");
            }
        }
    }
}
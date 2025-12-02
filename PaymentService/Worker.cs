using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using PaymentService.Models;

namespace PaymentService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _config;
        private IConnection? _connection;
        private IModel? _channel;

        public Worker(ILogger<Worker> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            await Task.Delay(3000, token);

            var factory = new ConnectionFactory
            {
                HostName = _config["RabbitMQ:Host"] ?? "localhost",
                UserName = _config["RabbitMQ:Username"] ?? "guest",
                Password = _config["RabbitMQ:Password"] ?? "guest"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare("order_exchange", ExchangeType.Direct, durable: true);
            _channel.ExchangeDeclare("payment_exchange", ExchangeType.Direct, durable: true);

            _channel.QueueDeclare("order_queue", durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind("order_queue", "order_exchange", "order.created");
            _channel.BasicQos(0, 1, false);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var retries = 0;

                try
                {
                    _logger.LogInformation("Received: {Message}", message);

                    var orderEvt = JsonSerializer.Deserialize<OrderCreatedEvent>(message);
                    if (orderEvt == null) throw new Exception("Invalid message");

                    Thread.Sleep(2000); // Payment processing

                    var success = new Random().Next(100) < 90;

                    var paymentEvt = new PaymentCompletedEvent
                    {
                        OrderId = orderEvt.OrderId,
                        CustomerEmail = orderEvt.CustomerEmail,
                        Amount = orderEvt.Amount,
                        Success = success,
                        Message = success ? "Payment successful" : "Payment failed",
                        ProcessedAt = DateTime.UtcNow
                    };

                    _logger.LogInformation("Payment {Status} for Order {OrderId}",
                        success ? "Success" : "Failed", orderEvt.OrderId);

                    var json = JsonSerializer.Serialize(paymentEvt);
                    var outBody = Encoding.UTF8.GetBytes(json);
                    var props = _channel.CreateBasicProperties();
                    props.Persistent = true;

                    _channel.BasicPublish("payment_exchange", "payment.completed", props, outBody);
                    _channel.BasicAck(ea.DeliveryTag, false);

                    _logger.LogInformation("Published payment event");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message");

                    if (retries < 3)
                    {
                        _channel.BasicNack(ea.DeliveryTag, false, true);
                        Thread.Sleep(2000 * (retries + 1));
                    }
                    else
                    {
                        _channel.BasicNack(ea.DeliveryTag, false, false);
                    }
                }
            };

            _channel.BasicConsume("order_queue", false, consumer);

            _logger.LogInformation("Payment Service started");

            await Task.Delay(Timeout.Infinite, token);
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}

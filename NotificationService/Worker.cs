using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using NotificationService.Models;

namespace NotificationService
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
            await Task.Delay(5000, token);

            var factory = new ConnectionFactory
            {
                HostName = _config["RabbitMQ:Host"] ?? "localhost",
                UserName = _config["RabbitMQ:Username"] ?? "guest",
                Password = _config["RabbitMQ:Password"] ?? "guest"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare("payment_exchange", ExchangeType.Direct, durable: true);
            _channel.QueueDeclare("notification_queue", durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind("notification_queue", "payment_exchange", "payment.completed");
            _channel.BasicQos(0, 1, false);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                try
                {
                    _logger.LogInformation("Received: {Message}", message);

                    var paymentEvt = JsonSerializer.Deserialize<PaymentCompletedEvent>(message);
                    if (paymentEvt == null)
                    {
                        _logger.LogWarning("Invalid message format");
                        _channel.BasicNack(ea.DeliveryTag, false, false);
                        return;
                    }

                    SendEmail(paymentEvt);
                    SendSMS(paymentEvt);

                    _channel.BasicAck(ea.DeliveryTag, false);
                    _logger.LogInformation("Notifications sent for Order {OrderId}", paymentEvt.OrderId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing notification");
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            _channel.BasicConsume("notification_queue", false, consumer);

            _logger.LogInformation("Notification Service started");

            await Task.Delay(Timeout.Infinite, token);
        }

        private void SendEmail(PaymentCompletedEvent evt)
        {
            _logger.LogInformation("=== EMAIL NOTIFICATION ===");
            _logger.LogInformation("To: {Email}", evt.CustomerEmail);
            _logger.LogInformation("Subject: Payment {Status} - Order #{OrderId}",
                evt.Success ? "Successful" : "Failed", evt.OrderId);

            if (evt.Success)
            {
                _logger.LogInformation(@"
Dear Customer,

Your payment of ${Amount} has been processed successfully.

Order ID: {OrderId}
Amount: ${Amount}
Status: Success

Thank you!
", evt.Amount, evt.OrderId, evt.Amount);
            }
            else
            {
                _logger.LogInformation(@"
Dear Customer,

Your payment could not be processed.

Order ID: {OrderId}
Amount: ${Amount}
Reason: {Message}

Please try again.
", evt.OrderId, evt.Amount, evt.Message);
            }

            _logger.LogInformation("=== EMAIL SENT ===");
        }

        private void SendSMS(PaymentCompletedEvent evt)
        {
            _logger.LogInformation("=== SMS NOTIFICATION ===");
            var sms = evt.Success
                ? $"Payment successful! Order #{evt.OrderId} - ${evt.Amount}"
                : $"Payment failed for Order #{evt.OrderId}. {evt.Message}";

            _logger.LogInformation("SMS to {Email}: {Message}", evt.CustomerEmail, sms);
            _logger.LogInformation("=== SMS SENT ===");
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}
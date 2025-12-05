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

            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _config["RabbitMQ:Host"] ?? "localhost",
                    UserName = _config["RabbitMQ:Username"] ?? "guest",
                    Password = _config["RabbitMQ:Password"] ?? "guest",
                    AutomaticRecoveryEnabled = true
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
                        _logger.LogInformation("NotificationService received PaymentCompletedEvent: {Message}", message);

                        var paymentEvt = JsonSerializer.Deserialize<PaymentCompletedEvent>(message);
                        if (paymentEvt == null)
                        {
                            _logger.LogWarning("Invalid PaymentCompletedEvent format");
                            _channel.BasicNack(ea.DeliveryTag, false, false);
                            return;
                        }

                        SendEmail(paymentEvt);
                        SendSMS(paymentEvt);

                        _channel.BasicAck(ea.DeliveryTag, false);
                        _logger.LogInformation("✓ All notifications sent successfully for Order {OrderId}", paymentEvt.OrderId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing notification");
                        _channel.BasicNack(ea.DeliveryTag, false, true);
                    }
                };

                _channel.BasicConsume("notification_queue", false, consumer);

                _logger.LogInformation("NotificationService started and listening for PaymentCompletedEvents");

                await Task.Delay(Timeout.Infinite, token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationService Worker failed to start");
            }
        }

        private void SendEmail(PaymentCompletedEvent evt)
        {
            _logger.LogInformation("📧 EMAIL NOTIFICATION");
            _logger.LogInformation("To: {Email}", evt.CustomerEmail);
            _logger.LogInformation("Subject: Payment {Status} - Order #{OrderId}",
                evt.Success ? "Successful ✓" : "Failed ✗", evt.OrderId);

            if (evt.Success)
            {
                _logger.LogInformation(@"
Dear Customer,

Your payment has been processed successfully! 🎉

Order Details:
  • Order ID: #{OrderId}
  • Amount: ${Amount:F2}
  • Status: SUCCESS ✓
  • Processed: {ProcessedAt:yyyy-MM-dd HH:mm:ss} UTC

Thank you for your business!
", evt.OrderId, evt.Amount, evt.ProcessedAt);
            }
            else
            {
                _logger.LogInformation(@"
Dear Customer,

Unfortunately, your payment could not be processed. ❌

Order Details:
  • Order ID: #{OrderId}
  • Amount: ${Amount:F2}
  • Status: FAILED
  • Reason: {Message}
  • Time: {ProcessedAt:yyyy-MM-dd HH:mm:ss} UTC

Please check your payment method and try again.
", evt.OrderId, evt.Amount, evt.Message, evt.ProcessedAt);
            }

            _logger.LogInformation("✓ Email sent successfully");
        }

        private void SendSMS(PaymentCompletedEvent evt)
        {
            _logger.LogInformation("📱 SMS NOTIFICATION");

            var sms = evt.Success
                ? $"✓ Payment successful! Order #{evt.OrderId} - ${evt.Amount:F2}. Thank you!"
                : $"✗ Payment failed for Order #{evt.OrderId}. {evt.Message} Please try again.";

            _logger.LogInformation("To: {Phone} (via {Email})", "Customer's Phone", evt.CustomerEmail);
            _logger.LogInformation("Message: {Sms}", sms);
            _logger.LogInformation("✓ SMS sent successfully");
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            _logger.LogInformation("NotificationService Worker disposed");
            base.Dispose();
        }
    }
}
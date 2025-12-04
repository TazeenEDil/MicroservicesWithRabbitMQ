using PaymentService.Interfaces;
using PaymentService.Models;
using PaymentService.Repositories;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace PaymentService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _config;
        private readonly IServiceProvider _serviceProvider;
        private IConnection? _connection;
        private IModel? _channel;

        public Worker(
            ILogger<Worker> logger,
            IConfiguration config,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _config = config;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            await Task.Delay(3000, token);

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

                _channel.ExchangeDeclare("order_exchange", ExchangeType.Direct, durable: true);
                _channel.ExchangeDeclare("payment_exchange", ExchangeType.Direct, durable: true);

                _channel.QueueDeclare("order_queue", durable: true, exclusive: false, autoDelete: false);
                _channel.QueueBind("order_queue", "order_exchange", "order.created");
                _channel.BasicQos(0, 1, false);

                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    int retryCount = 0;

                    // Check if message has been retried
                    if (ea.BasicProperties.Headers != null &&
                        ea.BasicProperties.Headers.ContainsKey("x-retry-count"))
                    {
                        retryCount = Convert.ToInt32(ea.BasicProperties.Headers["x-retry-count"]);
                    }

                    try
                    {
                        _logger.LogInformation("PaymentService received OrderCreatedEvent (Retry: {RetryCount}): {Message}",
                            retryCount, message);

                        var orderEvt = JsonSerializer.Deserialize<OrderCreatedEvent>(message);
                        if (orderEvt == null)
                        {
                            _logger.LogError("Invalid OrderCreatedEvent format");
                            _channel.BasicNack(ea.DeliveryTag, false, false); // Dead letter it
                            return;
                        }

                        // Create payment record
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var paymentRepo = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();

                            var payment = new Payment
                            {
                                OrderId = orderEvt.OrderId,
                                CustomerEmail = orderEvt.CustomerEmail,
                                Amount = orderEvt.Amount,
                                Status = "Processing",
                                ProcessedAt = DateTime.UtcNow,
                                RetryCount = retryCount
                            };

                            await paymentRepo.CreatePaymentAsync(payment);

                            // Simulate payment processing
                            _logger.LogInformation("Processing payment for Order {OrderId}...", orderEvt.OrderId);
                            await Task.Delay(2000); // Simulate payment gateway delay

                            // 90% success rate
                            var success = new Random().Next(100) < 90;

                            payment.Success = success;
                            payment.Status = success ? "Success" : "Failed";
                            payment.Message = success ? "Payment processed successfully" : "Payment gateway error";
                            payment.ProcessedAt = DateTime.UtcNow;

                            await paymentRepo.UpdatePaymentAsync(payment);

                            _logger.LogInformation("Payment {Status} for Order {OrderId} - Amount: ${Amount}",
                                success ? "SUCCESS" : "FAILED", orderEvt.OrderId, orderEvt.Amount);

                            // Publish PaymentCompletedEvent
                            var paymentEvt = new PaymentCompletedEvent
                            {
                                OrderId = orderEvt.OrderId,
                                CustomerEmail = orderEvt.CustomerEmail,
                                Amount = orderEvt.Amount,
                                Success = success,
                                Message = payment.Message,
                                ProcessedAt = payment.ProcessedAt
                            };

                            var json = JsonSerializer.Serialize(paymentEvt);
                            var outBody = Encoding.UTF8.GetBytes(json);
                            var props = _channel.CreateBasicProperties();
                            props.Persistent = true;
                            props.ContentType = "application/json";

                            _channel.BasicPublish("payment_exchange", "payment.completed", props, outBody);
                            _channel.BasicAck(ea.DeliveryTag, false);

                            _logger.LogInformation("PaymentCompletedEvent published for Order {OrderId}", orderEvt.OrderId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing payment for message (Retry: {RetryCount})", retryCount);

                        if (retryCount < 3)
                        {
                            // Retry logic
                            var retryDelay = (retryCount + 1) * 2000; // 2s, 4s, 6s
                            _logger.LogWarning("Retrying message after {Delay}ms (Attempt {Attempt}/3)",
                                retryDelay, retryCount + 1);

                            await Task.Delay(retryDelay);

                            // Re-publish with incremented retry count
                            var props = _channel.CreateBasicProperties();
                            props.Persistent = true;
                            props.Headers = new Dictionary<string, object>
                            {
                                { "x-retry-count", retryCount + 1 }
                            };

                            _channel.BasicPublish("order_exchange", "order.created", props, body);
                            _channel.BasicAck(ea.DeliveryTag, false);
                        }
                        else
                        {
                            _logger.LogError("Max retries reached for message. Moving to dead letter queue.");
                            _channel.BasicNack(ea.DeliveryTag, false, false);
                        }
                    }
                };

                _channel.BasicConsume("order_queue", false, consumer);

                _logger.LogInformation("PaymentService started and listening for OrderCreatedEvents");

                await Task.Delay(Timeout.Infinite, token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PaymentService Worker failed to start");
            }
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            _logger.LogInformation("PaymentService Worker disposed");
            base.Dispose();
        }
    }
}
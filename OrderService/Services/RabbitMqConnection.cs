/*using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace OrderService.Services
{
    public class RabbitMqConnection : IDisposable
    {
        private readonly IConnection _conn;
        public IModel Channel { get; }

        public RabbitMqConnection(IConfiguration cfg)
        {
            var factory = new ConnectionFactory
            {
                HostName = cfg["RabbitMQ:Host"] ?? "localhost",
                Port = int.Parse(cfg["RabbitMQ:Port"] ?? "5672"),
                UserName = cfg["RabbitMQ:User"] ?? "guest",
                Password = cfg["RabbitMQ:Password"] ?? "guest"
            };

            _conn = factory.CreateConnection();
            Channel = _conn.CreateModel();

            Channel.ExchangeDeclare(
                exchange: cfg["RabbitMQ:Exchange"],
                type: ExchangeType.Direct,
                durable: true
            );
        }

        public void Dispose()
        {
            Channel?.Close();
            _conn?.Close();
        }
    }
}
*/
using RabbitMQ.Client;

namespace OrderService.Services
{
    public class RabbitMqConnection : IDisposable
    {
        private readonly IConnection _connection;

        public RabbitMqConnection(IConfiguration config)
        {
            var factory = new ConnectionFactory
            {
                HostName = config["RabbitMQ:Host"] ?? "localhost",
                UserName = config["RabbitMQ:Username"] ?? "guest",
                Password = config["RabbitMQ:Password"] ?? "guest"
            };

            _connection = factory.CreateConnection();
        }

        public IModel CreateChannel() => _connection.CreateModel();

        public void Dispose() => _connection?.Close();
    }
}
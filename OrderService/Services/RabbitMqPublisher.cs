/*using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace OrderService.Services
{
    public class RabbitMqPublisher
    {
        private readonly RabbitMqConnection _conn;
        private readonly IConfiguration _cfg;
        private readonly ILogger<RabbitMqPublisher> _log;
        public RabbitMqPublisher(RabbitMqConnection conn, IConfiguration cfg, ILogger<RabbitMqPublisher> log)
        {
            _conn = conn; _cfg = cfg; _log = log;
        }
        public void PublishOrderCreated(object evt, string routingKey)
        {
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt));
            var props = _conn.Channel.CreateBasicProperties();
            props.DeliveryMode = 2; // persistent
            _conn.Channel.BasicPublish(exchange: _cfg["RabbitMQ:Exchange"],
                                       routingKey: routingKey,
                                       basicProperties: props,
                                       body: body);
            _log.LogInformation("Published event to exchange {exchange} with routingkey {rk}", _cfg["RabbitMQ:Exchange"], routingKey);
        }
    }
}
*/
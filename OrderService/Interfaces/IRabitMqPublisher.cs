namespace OrderService.Interfaces
{
    public interface IRabbitMqPublisher
    {
        void PublishOrderCreated(object evt);
    }
}
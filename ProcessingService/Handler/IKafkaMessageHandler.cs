namespace ProcessingService.Handler;

public interface IKafkaMessageHandler
{
    string Topic { get; }
    Task<bool> HandleAsync(string jsonMessage);
}
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProcessingService.Handler;
public class KafkaConsumerBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConsumerConfig _consumerConfig;
    private readonly List<string> _topicsToSubscribe;
    private readonly ILogger<KafkaConsumerBackgroundService> _logger;

    public KafkaConsumerBackgroundService(IServiceProvider serviceProvider,
        IConfiguration config,
        ILogger<KafkaConsumerBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        _consumerConfig = new ConsumerConfig
        {
            BootstrapServers = config["Kafka:BootstrapService"],
            GroupId = config["Kafka:GroupId"],
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var scope = _serviceProvider.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IKafkaMessageHandler>();
        _topicsToSubscribe = handlers.Select(h => h.Topic).Distinct().ToList();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("enter to ExecuteAsync function");
        await Task.Yield();

        using var consumer = new ConsumerBuilder<string, string>(_consumerConfig).Build();
        consumer.Subscribe(_topicsToSubscribe);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = consumer.Consume(stoppingToken);
                if (result == null) continue;

                using (var scope = _serviceProvider.CreateScope())
                { 
                    var handlers = scope.ServiceProvider.GetServices<IKafkaMessageHandler>();
                    var matchedHandler = handlers.FirstOrDefault(h => h.Topic == result.Topic);

                    if (matchedHandler != null)
                    {
                        bool isSuccess = await matchedHandler.HandleAsync(result.Message.Value);
                        if (isSuccess)
                        {
                            consumer.Commit(result);
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError($"error: {ex.Message}");
        }
        finally
        {
            consumer.Close();
        }
    }
}
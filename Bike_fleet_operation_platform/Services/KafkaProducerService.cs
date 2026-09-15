using Bike_fleet_operation_platform.Dtos;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bike_fleet_operation_platform.Services;

public class KafkaProducerServices
{
    private readonly string _bootstrapServices;
    private readonly IProducer<string, string> _producer;
    public KafkaProducerServices(string bootstrapServices)
    {
        _bootstrapServices = bootstrapServices;
        var config = new ProducerConfig { BootstrapServers = bootstrapServices };
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public void Flush(TimeSpan timeout)
    {
        _producer.Flush(timeout);
    }
    public async Task SendToKafkaAsync(string topicName, Message<string,string> content)
    {
        try
        {
            var result = await _producer.ProduceAsync(topicName, content);
            Console.WriteLine($"delivered station {content.Key} to partition {result.Partition.Value} at offset {result.Offset.Value}");
        }
        catch (ProduceException<string, string> ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"kafka Error for station {content.Key}: {ex.Error.Reason}");
            Console.ResetColor();
        }
    }
}
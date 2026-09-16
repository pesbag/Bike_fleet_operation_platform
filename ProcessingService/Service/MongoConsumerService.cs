//using Confluent.Kafka;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using MongoDB.Bson;
//using MongoDB.Driver;
//using ProcessingService.Dtos;
//using StackExchange.Redis;
//using System;
//using System.Text.Json;
//using System.Threading;
//using System.Threading.Tasks;

//namespace ProcessingService.Service;

//public class MongoConsumerService : BackgroundService
//{
//    private readonly ConsumerConfig _consumerConfig;
//    private readonly IMongoCollection<BsonDocument> _collection;
//    private readonly IDatabase _redisDb;
//    private readonly string _topic;
//    private readonly ILogger<MongoConsumerService> _logger;
//    private readonly JsonSerializerOptions _jsonOptions;
//    private readonly IMongoCollection<BsonDocument> _stationsMetadataCollection;

//    public MongoConsumerService(
//        ConsumerConfig consumerConfig,
//        IMongoDatabase database,
//        IConnectionMultiplexer redisMultiplexer,
//        IConfiguration configuration,
//        ILogger<MongoConsumerService> logger)
//    {
//        _consumerConfig = new ConsumerConfig(consumerConfig)
//        {
//            GroupId = "stations_status_mongo_group"
//        };
//        _collection = database.GetCollection<BsonDocument>("StationsStatus");
//        _redisDb = redisMultiplexer.GetDatabase();
//        _topic = configuration["Kafka:Topics:StationStatus"]
//            ?? throw new InvalidOperationException("Missing Kafka:Topics:StationStatus in configuration");
//        _logger = logger;                         
//        _stationsMetadataCollection = database.GetCollection<BsonDocument>("stationInformation");
//        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        _logger.LogInformation("MongoConsumerService started, listening to topic: {Topic}", _topic);

//        await Task.Run(async () =>
//        {
//            using var consumer = new ConsumerBuilder<string, string>(_consumerConfig).Build();
//            consumer.Subscribe(_topic);

//            try
//            {
//                while (!stoppingToken.IsCancellationRequested)
//                {
//                    ConsumeResult<string, string>? consumeResult = null;

//                    try
//                    {
//                        consumeResult = consumer.Consume(stoppingToken);
//                    }
//                    catch (OperationCanceledException) { break; }
//                    catch (ConsumeException ex)
//                    {
//                        _logger.LogError("Kafka consume error: {Reason}", ex.Error.Reason);
//                        continue;
//                    }

//                    if (string.IsNullOrWhiteSpace(consumeResult?.Message?.Value))
//                    {
//                        continue;
//                    }

//                    try
//                    {
//                        string rawJson = consumeResult.Message.Value;
//                        var document = BsonDocument.Parse(rawJson);

//                        string stationId = string.Empty;
//                        var messageKey = consumeResult.Message.Key;
//                        if (!string.IsNullOrEmpty(messageKey))
//                        {
//                            stationId = messageKey;
//                        }
//                        else if (document.Contains("Station_id"))
//                        {
//                            stationId = document["Station_id"].ToString() ?? string.Empty;
//                        }

//                        //if (string.IsNullOrEmpty(stationId))
//                        //{
//                        //    _logger.LogWarning("Skipping message: Station ID not found");
//                        //    consumer.Commit(consumeResult);
//                        //    continue;
//                        //}

//                        var incomingState = JsonSerializer.Deserialize<StationStatusDto>(rawJson, _jsonOptions);
//                        if (incomingState == null)
//                        {
//                            consumer.Commit(consumeResult);
//                            continue;
//                        }

//                        string redisKey = $"stations:state:{stationId}";
//                        RedisValue cachedStateJson = await _redisDb.StringGetAsync(redisKey);

//                        StationStatusDto? cachedState = null;
//                        if (cachedStateJson.HasValue)
//                        {
//                            cachedState = JsonSerializer.Deserialize<StationStatusDto>(cachedStateJson.ToString(), _jsonOptions);
//                        }

//                        bool isNewOrChanged = cachedState == null || incomingState.HasChangedFrom(cachedState);

//                        if (isNewOrChanged)
//                        {
//                            string redisExistingKey = $"stations:exists:{stationId}";
//                            bool stationExists = await _redisDb.KeyExistsAsync(redisExistingKey);
//                            if (!stationExists)
//                            {
//                                long totalStations = await _stationsMetadataCollection.CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty, cancellationToken: stoppingToken);

//                                var stationFilter = Builders<BsonDocument>.Filter.Eq("Station_id", stationId);
//                                stationExists = await _stationsMetadataCollection.Find(stationFilter).AnyAsync(stoppingToken);

//                                _logger.LogInformation("Checking StationId: '{StationId}'. Total docs in collection '{ColName}': {Count}. Found? {Found}",
//        stationId, _stationsMetadataCollection.CollectionNamespace.CollectionName, totalStations, stationExists);

//                                if (stationExists)
//                                {
//                                    await _redisDb.StringSetAsync(redisExistingKey, "1", TimeSpan.FromDays(7));
//                                }
//                            }
//                            if (!stationExists)
//                            {
//                                _logger.LogWarning("station {StationId} does not exist in system. skipping update", stationId);
//                                consumer.Commit(consumeResult);
//                                continue;
//                            }
//                            _logger.LogInformation("state changed or new station {StationId}. replacing in Mongo & Redis", stationId);

//                            string minimalStateJson = JsonSerializer.Serialize(incomingState);
//                            await _redisDb.StringSetAsync(redisKey, minimalStateJson);

//                            var filter = Builders<BsonDocument>.Filter.Eq("Station_id", stationId);
//                            var options = new ReplaceOptions { IsUpsert = true };

//                            await _collection.ReplaceOneAsync(filter, document, options, cancellationToken: stoppingToken);
//                        }
//                        else
//                        {
//                            _logger.LogDebug("No state change detected for station {stationId}. skipping DB write", stationId);
//                        }
//                        consumer.Commit(consumeResult);
//                    }
//                    catch (JsonException ex)
//                    {
//                        _logger.LogError("malformed json message: {error}", ex.Message);
//                        consumer.Commit(consumeResult);
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.LogError("error handling message: {error}", ex.Message);
//                    }
//                }
//            }
//            finally
//            {
//                _logger.LogInformation("closing MongoConsumerService...");
//                consumer.Close();
//            }
//        }, stoppingToken);
//    }
//}


using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using ProcessingService.DataDbContext;
using ProcessingService.Dtos;
using StackExchange.Redis;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessingService.Service;

public class MongoConsumerService : BackgroundService
{
    private readonly ConsumerConfig _consumerConfig;
    private readonly IMongoCollection<BsonDocument> _collection;
    private readonly IDatabase _redisDb;
    private readonly string _topic;
    private readonly ILogger<MongoConsumerService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly IServiceScopeFactory _scopeFactory;

    public MongoConsumerService(
        ConsumerConfig consumerConfig,
        IMongoDatabase database,
        IConnectionMultiplexer redisMultiplexer,
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        ILogger<MongoConsumerService> logger)
    {
        _consumerConfig = new ConsumerConfig(consumerConfig)
        {
            GroupId = "stations_status_mongo_group"
        };
        _collection = database.GetCollection<BsonDocument>("StationsStatus");
        _redisDb = redisMultiplexer.GetDatabase();
        _topic = configuration["Kafka:Topics:StationStatus"]
            ?? throw new InvalidOperationException("Missing Kafka:Topics:StationStatus in configuration");
        _logger = logger;
        _scopeFactory = scopeFactory;
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MongoConsumerService started, listening to topic: {Topic}", _topic);

        await Task.Run(async () =>
        {
            using var consumer = new ConsumerBuilder<string, string>(_consumerConfig).Build();
            consumer.Subscribe(_topic);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    ConsumeResult<string, string>? consumeResult = null;

                    try
                    {
                        consumeResult = consumer.Consume(stoppingToken);
                    }
                    catch (OperationCanceledException) { break; }
                    catch (ConsumeException ex)
                    {
                        _logger.LogError("Kafka consume error: {Reason}", ex.Error.Reason);
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(consumeResult?.Message?.Value))
                    {
                        continue;
                    }

                    try
                    {
                        string rawJson = consumeResult.Message.Value;
                        var document = BsonDocument.Parse(rawJson);

                        string stationId = string.Empty;
                        var messageKey = consumeResult.Message.Key;
                        if (!string.IsNullOrEmpty(messageKey))
                        {
                            stationId = messageKey;
                        }
                        else if (document.Contains("Station_id"))
                        {
                            stationId = document["Station_id"].ToString() ?? string.Empty;
                        }
                        else if (document.Contains("station_id"))
                        {
                            stationId = document["station_id"].ToString() ?? string.Empty;
                        }

                        if (string.IsNullOrEmpty(stationId))
                        {
                            consumer.Commit(consumeResult);
                            continue;
                        }

                        var incomingState = JsonSerializer.Deserialize<StationStatusDto>(rawJson, _jsonOptions);
                        if (incomingState == null)
                        {
                            consumer.Commit(consumeResult);
                            continue;
                        }

                        string redisKey = $"stations:state:{stationId}";
                        RedisValue cachedStateJson = await _redisDb.StringGetAsync(redisKey);

                        StationStatusDto? cachedState = null;
                        if (cachedStateJson.HasValue)
                        {
                            cachedState = JsonSerializer.Deserialize<StationStatusDto>(cachedStateJson.ToString(), _jsonOptions);
                        }

                        bool isNewOrChanged = cachedState == null || incomingState.HasChangedFrom(cachedState);

                        if (isNewOrChanged)
                        {
                            string redisExistingKey = $"stations:exists:{stationId}";
                            bool stationExists = await _redisDb.KeyExistsAsync(redisExistingKey);

                            if (!stationExists)
                            {
                                using var scope = _scopeFactory.CreateScope();
                                var dbContext = scope.ServiceProvider.GetRequiredService<StationDbContext>();
                                stationExists = await dbContext.stationInformation.FindAsync(new object[] { stationId }, stoppingToken) != null;

                                if (stationExists)
                                {
                                    await _redisDb.StringSetAsync(redisExistingKey, "1", TimeSpan.FromDays(7));
                                }
                            }

                            if (!stationExists)
                            {
                                _logger.LogWarning("station {StationId} does not exist in system. skipping update", stationId);
                                consumer.Commit(consumeResult);
                                continue;
                            }

                            _logger.LogInformation("state changed or new station {StationId}. replacing in Mongo & Redis", stationId);

                            string minimalStateJson = JsonSerializer.Serialize(incomingState);
                            await _redisDb.StringSetAsync(redisKey, minimalStateJson);

                            var filter = Builders<BsonDocument>.Filter.Or(
                                Builders<BsonDocument>.Filter.Eq("Station_id", stationId),
                                Builders<BsonDocument>.Filter.Eq("station_id", stationId)
                            );
                            var options = new ReplaceOptions { IsUpsert = true };

                            await _collection.ReplaceOneAsync(filter, document, options, cancellationToken: stoppingToken);
                        }
                        else
                        {
                            _logger.LogDebug("No state change detected for station {stationId}. skipping DB write", stationId);
                        }

                        consumer.Commit(consumeResult);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError("malformed json message: {error}", ex.Message);
                        consumer.Commit(consumeResult);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("error handling message: {error}", ex.Message);
                    }
                }
            }
            finally
            {
                _logger.LogInformation("closing MongoConsumerService...");
                consumer.Close();
            }
        }, stoppingToken);
    }
}
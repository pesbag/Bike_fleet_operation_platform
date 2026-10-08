using StackExchange.Redis;
using System.Text.Json;

namespace ProcessingService.RedisCash;
public class SensorCurrentStateStore
{
    private readonly IDatabase _redisDb;
    public SensorCurrentStateStore(IDatabase redisDb)
    {
        _redisDb = redisDb;
    }

    public async Task SaveLatestStateAsync(string stationId, string rawJsonPayload)
    {
        // 1. הגדרת המפתח הייחודי
        string redisKey = $"station:status:{stationId}";

        // 2. שמירת המחרוזת ברדיס (StringSet דורס ומעדכן את המצב האחרון)
        // אם המפתח כבר היה קיים - הוא פשוט מתעדכן לערך החדש!
        await _redisDb.StringSetAsync(redisKey, rawJsonPayload);
    }

    public async Task<string?> GetLatestStateAsync(string stationId)
    {
        string redisKey = $"station:status:{stationId}";

        // שליפה מיידית בזמן של פחות ממילי-שנייה
        RedisValue value = await _redisDb.StringGetAsync(redisKey);

        return value.HasValue ? value.ToString() : null;
    }
}
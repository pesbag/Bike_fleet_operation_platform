//using ProcessingService.DataDbContext;
//using ProcessingService.Dtos;
//using System.Text.Json;

//namespace ProcessingService.Handler;

//public class StationInformationHandler : IKafkaMessageHandler
//{
//    private readonly StationDbContext _context;
//    public string Topic => "bike.station-information";

//    public StationInformationHandler(StationDbContext context)
//    {
//        _context = context;
//    }

//    public async Task<bool> HandleAsync(string jsonMessage)
//    {
//        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
//        var dto = JsonSerializer.Deserialize<StationInformationDto>(jsonMessage, options);
//        if (dto is null) return false;

//        var existing = await _context.stationInformation.FindAsync(dto.Station_id);
//        if (existing is null)
//            await _context.stationInformation.AddAsync(dto);
//        else
//            _context.Entry(existing).CurrentValues.SetValues(dto);

//        await _context.SaveChangesAsync();
//        return true;
//    }
//}

using ProcessingService.DataDbContext;
using ProcessingService.Dtos;
using StackExchange.Redis;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProcessingService.Handler;

public class StationInformationHandler : IKafkaMessageHandler
{
    private readonly StationDbContext _context;
    private readonly IDatabase _redisDb;
    public string Topic => "bike.station-information";

    public StationInformationHandler(StationDbContext context, IDatabase redisDb)
    {
        _context = context;
        _redisDb = redisDb;
    }

    public async Task<bool> HandleAsync(string jsonMessage)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var dto = JsonSerializer.Deserialize<StationInformationDto>(jsonMessage, options);
        if (dto is null) return false;

        var existing = await _context.stationInformation.FindAsync(dto.Station_id);
        if (existing is null)
            await _context.stationInformation.AddAsync(dto);
        else
            _context.Entry(existing).CurrentValues.SetValues(dto);

        await _context.SaveChangesAsync();

        await _redisDb.StringSetAsync($"stations:exists:{dto.Station_id}", "1", TimeSpan.FromDays(7));

        return true;
    }
}
using ProcessingService.DataDbContext;
using ProcessingService.Dtos;
using System.Text.Json;

namespace ProcessingService.Handler;

public class StationInformationHandler : IKafkaMessageHandler
{
    private readonly StationDbContext _context;
    public string Topic => "bike.station-information";

    public StationInformationHandler(StationDbContext context)
    {
        _context = context;
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
        return true;
    }
}
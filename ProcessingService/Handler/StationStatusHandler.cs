using Microsoft.Extensions.Options;
using ProcessingService.Dtos;
using MongoDB.Driver;
using ProcessingService.DataDbContext;
namespace ProcessingService.Handler;

public class StationStatusHandler(IOptions<StationDbContext> stationStatus)
{
}

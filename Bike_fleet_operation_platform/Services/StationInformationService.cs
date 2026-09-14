using Bike_fleet_operation_platform.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http.Json;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Bike_fleet_operation_platform.Services;

public class StationInformationService
{
    private readonly IHttpClientFactory _httpClientFactory = null!;
    private readonly IConfiguration _configuration = null!;
    private readonly ILogger<StationInformationService> _logger = null!;

    public StationInformationService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<StationInformationService> logger) =>
        (_httpClientFactory, _configuration, _logger) =
            (httpClientFactory, configuration, logger);

    public async Task<StationInformationDto[]> GetStationsInformationAsync(int userId)
    {
        // Create the client
        string? httpClientName = _configuration["StationInformation"];
        HttpClient client = _httpClientFactory.CreateClient(httpClientName ?? "");

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            using JsonDocument document = await client.GetFromJsonAsync<JsonDocument>("station_information.json")
                ?? throw new InvalidOperationException("failed to read json document");

            JsonElement stationsElement = document.RootElement
            .GetProperty("data")
            .GetProperty("stations");

            StationInformationDto[]? stationsInformation = stationsElement.Deserialize<StationInformationDto[]>(options) ?? [];
            Console.WriteLine($"succesfully recived {stationsInformation.Length} stations information");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"the error: {ex.Message}");
            _logger.LogError("Error getting something fun to say: {Error}", ex);
        }
        return [];
    }
}

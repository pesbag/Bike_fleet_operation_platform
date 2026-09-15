using Bike_fleet_operation_platform.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

string? httpClientName = builder.Configuration["StationInformation"];
ArgumentException.ThrowIfNullOrEmpty(httpClientName);

builder.Services.AddHttpClient(
    httpClientName,
    client =>
    {
        client.BaseAddress = new Uri("https://gbfs.lyft.com/gbfs/2.3/bkn/en/");
        client.DefaultRequestHeaders.UserAgent.ParseAdd("dotnet-docs");
    });

builder.Services.AddTransient<StationInformationService>();
builder.Services.AddTransient<StationStatusService>();

using IHost host = builder.Build();


var statusService = host.Services.GetRequiredService<StationStatusService>();
var stationsStatus = await statusService.GetStationsStatusAsync();

var informationService = host.Services.GetRequiredService<StationInformationService>();
var stationsInformation=await informationService.GetStationsInformationAsync();
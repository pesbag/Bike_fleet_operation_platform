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

using IHost host = builder.Build();


var service = host.Services.GetRequiredService<StationInformationService>();
await service.GetStationsInformationAsync(1);
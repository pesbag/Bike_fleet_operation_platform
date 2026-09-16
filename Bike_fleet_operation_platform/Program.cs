//using Bike_fleet_operation_platform.Services;
//using Bike_fleet_operation_platform.Workers;
//using Confluent.Kafka;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using System;
//using System.Text.RegularExpressions;
//using static Confluent.Kafka.ConfigPropertyNames;

//HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

//var config = new ConfigurationBuilder()
//    .SetBasePath(Directory.GetCurrentDirectory())
//    .AddJsonFile("appsettings.json", optional: true)
//    .Build();

//string connectionString = config["ConnectionStrings:DefaultConnection"]!;
//string bootstrapServices = config["Kafka:BootstrapServices"]!;
//string stationsInformationTopic = config["Kafka:Topics:StationInformation"]!;
//string stationStatusTopic = config["Kafka:Topics:StationStatus"]!;
//string vehicleTypeTopic = config["Kafka:Topics:VehicleTypes"]!;

//string? httpClientName = builder.Configuration["StationInformation"];
//ArgumentException.ThrowIfNullOrEmpty(httpClientName);

//builder.Services.AddHttpClient(
//    httpClientName,
//    client =>
//    {
//        client.BaseAddress = new Uri("https://gbfs.lyft.com/gbfs/2.3/bkn/en/");
//        client.DefaultRequestHeaders.UserAgent.ParseAdd("dotnet-docs");
//    });

//builder.Services.AddHostedService<StationsPollingWorker>();
//builder.Services.AddTransient<StationInformationService>();
//builder.Services.AddTransient<StationStatusService>();
//builder.Services.AddTransient<VehicleTypesService>();
//builder.Services.AddSingleton<KafkaProducerServices>(sp=> new KafkaProducerServices(bootstrapServices));

//using IHost host = builder.Build();

//var vehicleTypeService = host.Services.GetRequiredService<VehicleTypesService>();
//var vehicleTypes = await vehicleTypeService.GetVehicleTypesDtoAsync(vehicleTypeTopic);

//var statusService = host.Services.GetRequiredService<StationStatusService>();
//var stationsStatus = await statusService.GetStationsStatusAsync(stationStatusTopic);

//var informationService = host.Services.GetRequiredService<StationInformationService>();
//var stationsInformation=await informationService.GetStationsInformationAsync(stationsInformationTopic);

using Bike_fleet_operation_platform.Services;
using Bike_fleet_operation_platform.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// 1. קריאת קונפיגורציה ישירות מ-appsettings.json
string bootstrapServices = builder.Configuration["Kafka:BootstrapServices"]
    ?? throw new InvalidOperationException("Missing Kafka:BootstrapServices in configuration");

string stationsInformationTopic = builder.Configuration["Kafka:Topics:StationInformation"]
    ?? "bike.station-information";

string stationStatusTopic = builder.Configuration["Kafka:Topics:StationStatus"]
    ?? "bike.station-status";

string vehicleTypeTopic = builder.Configuration["Kafka:Topics:VehicleTypes"]
    ?? "bike.vehicle-types";

string httpClientName = builder.Configuration["StationInformation"]
    ?? "LyftStationClient";

// 2. הגדרת ה-HttpClient
builder.Services.AddHttpClient(
    httpClientName,
    client =>
    {
        client.BaseAddress = new Uri("https://gbfs.lyft.com/gbfs/2.3/bkn/en/");
        client.DefaultRequestHeaders.UserAgent.ParseAdd("dotnet-docs");
    });

// 3. רישום שירותי התשתית והשליפה
builder.Services.AddSingleton<KafkaProducerServices>(sp => new KafkaProducerServices(bootstrapServices));
builder.Services.AddTransient<StationInformationService>();
builder.Services.AddTransient<StationStatusService>();
builder.Services.AddTransient<VehicleTypesService>();

// 4. רישום ה-Worker שמתזמן את הטיקים ברקע
builder.Services.AddHostedService<StationsPollingWorker>();

using IHost host = builder.Build();

// 5. ריצה ראשונית: שולחים קודם את המטא-דאטה (מידע תחנות ורכבים) כדי שמונגו יתאכלס
var informationService = host.Services.GetRequiredService<StationInformationService>();
await informationService.GetStationsInformationAsync(stationsInformationTopic);

var vehicleTypeService = host.Services.GetRequiredService<VehicleTypesService>();
await vehicleTypeService.GetVehicleTypesDtoAsync(vehicleTypeTopic);

// רק כעת שולחים את הסטטוסים הראשוניים
var statusService = host.Services.GetRequiredService<StationStatusService>();
await statusService.GetStationsStatusAsync(stationStatusTopic);

// 6. השארת התהליך פעיל והפעלת ה-StationsPollingWorker
await host.RunAsync();
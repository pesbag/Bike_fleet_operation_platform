using Bike_fleet_operation_platform.DataDbContext;
using Bike_fleet_operation_platform.Services;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Text.RegularExpressions;
using static Confluent.Kafka.ConfigPropertyNames;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .Build();

string connectionString = config["ConnectionStrings:DefaultConnection"]!;
string bootstrapServices = config["Kafka:BootstrapServices"]!;
string stationsInformationTopic = config["Kafka:Topics:StationInformation"] ?? "stationsInformation";
string stationStatusTopic = config["Kafka:Topics:StationStatus"] ?? "stationsStatus";
string vehicleTypeTopic = config["Kafka:Topics:VehicleTypes"] ?? "vehicleTypes";

string? httpClientName = builder.Configuration["StationInformation"];
ArgumentException.ThrowIfNullOrEmpty(httpClientName);

builder.Services.AddHttpClient(
    httpClientName,
    client =>
    {
        client.BaseAddress = new Uri("https://gbfs.lyft.com/gbfs/2.3/bkn/en/");
        client.DefaultRequestHeaders.UserAgent.ParseAdd("dotnet-docs");
    });


builder.Services.AddDbContext<StationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

builder.Services.AddTransient<StationInformationService>();
builder.Services.AddTransient<StationStatusService>();
builder.Services.AddTransient<VehicleTypesService>();
builder.Services.AddSingleton<KafkaProducerServices>(sp=> new KafkaProducerServices(bootstrapServices));

using IHost host = builder.Build();

var vehicleTypeService = host.Services.GetRequiredService<VehicleTypesService>();
var vehicleTypes = await vehicleTypeService.GetVehicleTypesDtoAsync(vehicleTypeTopic);

var statusService = host.Services.GetRequiredService<StationStatusService>();
var stationsStatus = await statusService.GetStationsStatusAsync(stationStatusTopic);

var informationService = host.Services.GetRequiredService<StationInformationService>();
var stationsInformation=await informationService.GetStationsInformationAsync(stationsInformationTopic);




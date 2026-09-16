using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProcessingService.DataDbContext;
using ProcessingService.Handler;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// 1. מחרוזת התחברות ל-MySQL
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection in appsettings.json");

// 2. רישום DbContext (Scoped)
builder.Services.AddDbContext<StationDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))));

// 3. רישום ה-Handler (Scoped)
builder.Services.AddScoped<StationInformationHandler>();

// 4. רישום שירות הצרכן של קפקא שירוץ ברקע לתמיד
builder.Services.AddHostedService<KafkaConsumerBackgroundService>();
builder.Services.AddScoped<IKafkaMessageHandler, StationInformationHandler>();
builder.Services.AddScoped<IKafkaMessageHandler, VehicleTypesHandler>();
using IHost host = builder.Build();

// מריץ את האפליקציה ומחזיק אותה מאזינה ברקע
await host.RunAsync();
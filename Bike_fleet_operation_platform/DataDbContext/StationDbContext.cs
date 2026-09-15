using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bike_fleet_operation_platform.Dtos;
using Microsoft.EntityFrameworkCore;
namespace Bike_fleet_operation_platform.DataDbContext;

public class StationDbContext: DbContext
{
    //private readonly DbContextOptions<StationDbContext> _connection;
    public StationDbContext(DbContextOptions<StationDbContext> options):base(options) {}
    public DbSet<StationInformationDto> stationInformation { get; set; }
    public DbSet<VehicleTypesDto> vehicleTypes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<StationInformationDto>()
            .HasKey(e => e.Station_id);
        modelBuilder.Entity<VehicleTypesDto>()
            .HasKey(e => e.Vehicle_type_id);

    }
}
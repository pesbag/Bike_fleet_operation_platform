using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProcessingService.Dtos;
using Microsoft.EntityFrameworkCore;
namespace ProcessingService.DataDbContext;

public class StationDbContext: DbContext
{
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
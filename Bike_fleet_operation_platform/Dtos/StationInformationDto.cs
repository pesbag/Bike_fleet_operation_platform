using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bike_fleet_operation_platform.Dtos;

public class StationInformationDto
{
    public long station_id { get; set; }
    public string name { get; set; } = string.Empty;
    public double lat { get; set; }
    public double lon { get; set; }
    public int capacity { get; set; }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bike_fleet_operation_platform.Dtos;

public class StationStatusDto
{
    public string station_id { get; set; } = string.Empty;
    public int nun_vehicles_available { get; set; }
    public int num_docs_available { get; set; }
    public int is_renting { get; set; }
    public int is_returning { get; set; }
    public long last_reported { get; set; }
}

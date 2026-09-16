using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessingService.Dtos;

public class StationStatusDto
{
    [Required]
    public string Station_id { get; set; } = string.Empty;
    [Range(0,int.MaxValue)]
    public int Num_bikes_available { get; set; }
    [Range(0, int.MaxValue)]
    public int Nun_vehicles_available { get; set; }
    [Range(0, int.MaxValue)]
    public int Num_docs_available { get; set; }
    public int Is_renting { get; set; }
    public int Is_returning { get; set; }
    public long Last_reported { get; set; }
}

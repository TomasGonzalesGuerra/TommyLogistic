using System;
using System.Collections.Generic;
using System.Text;

namespace TommyLogistic.Shared.DTOs.Drivers;

public class DriverProfileUpdateDTO
{
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string Placa { get; set; } = null!;
    public string? PhotoBase64 { get; set; }
}

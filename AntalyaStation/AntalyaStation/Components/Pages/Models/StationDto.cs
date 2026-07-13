namespace AntalyaStation.Models;

public class StationDto
{
    public string Id { get; set; }
    public string StationNumber { get; set; }
    public string StationName { get; set; }
    public string Address { get; set; }
    public int TotalSockets { get; set; }
    public string District { get; set; } // Bu satırı ekle
    public string OperatorName { get; set; } // Bu satırı ekle
    public string ServiceType { get; set; }
}
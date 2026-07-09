namespace AntalyaStation.API.DTOs;

public class StationDto
{
    public string Id { get; set; }
    public string StationNumber { get; set; }
    public string StationName { get; set; }
    public string Address { get; set; }
    public int TotalSockets { get; set; } // Sadece soket sayısını dönerek optimizasyon yapıyoruz
}
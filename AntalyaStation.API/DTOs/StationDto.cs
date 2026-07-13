using System.Collections.Generic;

namespace AntalyaStation.API.DTOs
{
    public class StationDto
    {
        public string? Id { get; set; }
        public string StationNumber { get; set; } = string.Empty;
        public string StationName { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;        
        public string OperatorName { get; set; } = string.Empty; 
        public string ServiceType { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = "ANTALYA"; 
        public string District { get; set; } = string.Empty; 
        public int TotalSockets { get; set; }
        
        public bool IsGreenCharging { get; set; } 
        public bool IsSmartCharging { get; set; } 

        public List<SocketDto> Sockets { get; set; } = new();
    }

    public class SocketDto
    {
        public string SocketNumber { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Power { get; set; } = string.Empty;
    }
}
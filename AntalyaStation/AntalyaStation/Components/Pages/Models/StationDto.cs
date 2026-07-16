using System.Collections.Generic;

namespace AntalyaStation.API.DTOs
{
    public class StationDto
    {
        public string? SearchText { get; set; }
        public string? Id { get; set; }
        public string StationNumber { get; set; } = string.Empty;
        public string StationName { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string OperatorNetwork { get; set; } = string.Empty;
        public string OperatorStation { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = "ANTALYA";
        public string District { get; set; } = string.Empty;
        public int TotalSockets { get; set; }

        public bool IsGreenCharging { get; set; }
        public bool IsSmartCharging { get; set; }

        public List<SocketDto> Sockets { get; set; } = new();
        public DateTime AddedDate { get; set; } = DateTime.Now;
        public bool IsNew { get; set; } = true;
        public double TotalPower { get; set; }
        public int SocketCount { get; set; }
        public string Status { get; set; } = "Active";
    }

    public class SocketDto
    {
        public string SocketNumber { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public double Power { get; set; } // 🎯 string'den double'a çevrildi
    }
}
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AntalyaStation.API.Models
{
    [BsonIgnoreExtraElements]
    public class Station
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        
        public string StationNumber { get; set; } = string.Empty;
        public string StationName { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string OperatorNetwork  { get; set; } = string.Empty; 
        public string OperatorStation { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;  
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = "ANTALYA";
        public string District { get; set; } = string.Empty;
        
        public int TotalSockets { get; set; }
        
        public bool IsGreenCharging { get; set; }
        public bool IsSmartCharging { get; set; }

        public List<Socket> Sockets { get; set; } = new();
        // 🕒 Verinin veritabanına kaydedildiği tarih
        public DateTime AddedDate { get; set; } = DateTime.UtcNow;

        // 🆕 Excel'den veya formdan yeni eklenenleri işaretleyeceğimiz bayrak
        public bool IsNew { get; set; } = true;
    }

    public class Socket
    {
        public string SocketNumber { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // AC/DC
        public string Power { get; set; } = string.Empty; // kW
    }
}
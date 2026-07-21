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
        public string OperatorNetwork { get; set; } = string.Empty;
        public string OperatorStation { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = "ANTALYA";
        public string District { get; set; } = string.Empty;

        public int TotalSockets { get; set; }

        public List<Socket> Sockets { get; set; } = new();
        public DateTime AddedDate { get; set; } = DateTime.Now;
        public bool IsNew { get; set; } = true;
        public double TotalPower { get; set; }
        public int SocketCount { get; set; }
        public string Status { get; set; } = "Active";
        // 🟢 EKLENMESİ GEREKEN ALAN:
        public bool IsActive { get; set; } = true;
        public DateTime? DeactivatedDate { get; set; }
        // Append these two tracking fields to your existing Station class inside Station.cs
        public string? ImportBatchId { get; set; }
        public DateTime? FileUploadedDate { get; set; }
        
        // 🆕 Harita için konum bilgisi. Excel/manuel girişte doldurulmazsa 0 kalır
                // ve bu istasyon haritada gösterilmez (Map.razor bu kontrolü yapıyor).
                public double Latitude { get; set; }
                public double Longitude { get; set; }
    }

    public class Socket
    {
        public string SocketNumber { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public double Power { get; set; } // 🎯 string'den double'a çevrildi
    }
}
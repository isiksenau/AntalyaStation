namespace AntalyaStation.API.Models;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class Station
{
    [BsonId] //// Bu alanın MongoDB'deki benzersiz anahtar (_id) olduğunu söyler.
    [BsonRepresentation(BsonType.ObjectId)] //// Mongo'daki özel ID tipini C# string tipine çevirir.
    public string? Id { get; set; } // MongoDB'nin kendi ID'si
    public string StationNumber { get; set; } // İstasyon No
    public string StationName { get; set; }
    public string Brand { get; set; }
    public string Address { get; set; }
    
    // Bir istasyonda birden fazla soket olabilir
    public List<Socket> Sockets { get; set; } = new();
}
namespace AntalyaStation.API.Models
{
    using MongoDB.Bson;
    using MongoDB.Bson.Serialization.Attributes;

    public class Station
    {
        [BsonId] // Bu alan MongoDB için benzersiz kimlik olacak
        [BsonRepresentation(BsonType.ObjectId)] // MongoDB bunu ObjectId (string) olarak saklayacak
        public string Id { get; set; }

        public string OperatorName { get; set; }
        public string StationName { get; set; }
        public string Province { get; set; }
        public string District { get; set; }
        public string Address { get; set; }
    }
}
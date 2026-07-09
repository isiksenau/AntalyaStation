namespace AntalyaStation.API.Data
{
    // appsettings.json'daki verileri buraya eşleyeceğiz
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; } // Veritabanı linki
        public string DatabaseName { get; set; }     // Veritabanı ismi
    }
}
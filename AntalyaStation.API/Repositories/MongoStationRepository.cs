using AntalyaStation.API.Data;
using AntalyaStation.API.DTOs;
using AntalyaStation.API.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AntalyaStation.API.Repositories
{
    public class MongoStationRepository : IStationRepository
    {
        private readonly IMongoCollection<Station> _stations;

        public MongoStationRepository(IOptions<MongoDbSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _stations = database.GetCollection<Station>("Stations");
        }

        // Temel listeleyicimiz
        public async Task<(List<Station> Data, int TotalCount)> GetPagedStationsAsync(int pageNumber, int pageSize)
        {
            var totalCount = (int)await _stations.CountDocumentsAsync(_ => true);
            var data = await _stations.Find(_ => true)
                .Skip((pageNumber - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return (data, totalCount);
        }

       public async Task<(List<Station> Data, int TotalCount)> GetFilteredStationsAsync(StationFilterDto filter, int pageNumber, int pageSize)
{
    var builder = Builders<Station>.Filter;
    var filterDefinition = builder.Empty;

    // 🟢 1. BİRLEŞİK ARAMA (İsim, Numara veya Marka içinde KISMEN geçiyorsa bile getirir)
    if (!string.IsNullOrWhiteSpace(filter.SearchText))
    {
        var searchRegex = new BsonRegularExpression(filter.SearchText, "i"); // "i" = Büyük/Küçük harf duyarsız
        
        var nameFilter = builder.Regex(s => s.StationName, searchRegex);
        var numberFilter = builder.Regex(s => s.StationNumber, searchRegex);
        var brandFilter = builder.Regex(s => s.Brand, searchRegex); // Markayı da genel aramaya dahil ettik

        filterDefinition &= builder.Or(nameFilter, numberFilter, brandFilter);
    }

    // 🟢 2. MARKA FİLTRESİ (Gelişmiş - Kısmi arama toleranslı)
    if (!string.IsNullOrWhiteSpace(filter.Brand))
    {
        filterDefinition &= builder.Regex(s => s.Brand, new BsonRegularExpression(filter.Brand, "i"));
    }

    // 🟢 3. ŞARJ AĞI İŞLETMECİSİ (Çalışmama sorunu çözüldü - Kısmi arama toleranslı)
    if (!string.IsNullOrWhiteSpace(filter.OperatorNetwork))
    {
        filterDefinition &= builder.Regex(s => s.OperatorNetwork, new BsonRegularExpression(filter.OperatorNetwork, "i"));
    }

    // 🟢 4. İL FİLTRESİ
    if (!string.IsNullOrWhiteSpace(filter.City)) 
    {
        filterDefinition &= builder.Regex(s => s.City, new BsonRegularExpression($"^{filter.City}$", "i"));
    }
        
    // 🟢 5. İLÇE FİLTRESİ (Çalışmama sorunu çözüldü - Harf duyarlılığı kaldırıldı)
    if (!string.IsNullOrWhiteSpace(filter.District)) 
    {
        filterDefinition &= builder.Regex(s => s.District, new BsonRegularExpression(filter.District, "i"));
    }

    if (!string.IsNullOrWhiteSpace(filter.OperatorStation)) 
        filterDefinition &= builder.Regex(s => s.OperatorStation, new BsonRegularExpression(filter.OperatorStation, "i"));

    if (!string.IsNullOrWhiteSpace(filter.ServiceType)) 
        filterDefinition &= builder.Regex(s => s.ServiceType, new BsonRegularExpression(filter.ServiceType, "i"));

    if (!string.IsNullOrWhiteSpace(filter.StationType))
    {
        filterDefinition &= builder.ElemMatch(s => s.Sockets, 
            Builders<Socket>.Filter.Regex(sock => sock.Type, new BsonRegularExpression(filter.StationType, "i")));
    }

    if (filter.IsGreenCharging.HasValue) 
        filterDefinition &= builder.Eq(s => s.IsGreenCharging, filter.IsGreenCharging.Value);
    
    if (filter.IsSmartCharging.HasValue) 
        filterDefinition &= builder.Eq(s => s.IsSmartCharging, filter.IsSmartCharging.Value);

    // Veriyi çekme ve sayfalama adımları (.NET 10 uyumlu optimizasyonlu)
    var totalCount = (int)await _stations.CountDocumentsAsync(filterDefinition);
    var data = await _stations.Find(filterDefinition)
        .Skip((pageNumber - 1) * pageSize)
        .Limit(pageSize)
        .ToListAsync();

    return (data, totalCount);
}

        // Toplu Ekleme
        public async Task InsertManyAsync(List<Station> stations)
        {
            if (stations != null && stations.Any())
            {
                await _stations.InsertManyAsync(stations);
            }
        }

        // 🆕 YENİ: Tekli İstasyon Ekleme (POST)
        public async Task AddAsync(Station station)
        {
            await _stations.InsertOneAsync(station);
        }

        // 🆕 YENİ: İstasyon Güncelleme (PUT)
        public async Task<bool> UpdateAsync(string id, Station station)
        {
            // MongoDB'deki mevcut dökümanı yenisiyle değiştiriyoruz
            var result = await _stations.ReplaceOneAsync(s => s.Id == id, station);
            return result.ModifiedCount > 0;
        }

        // 🆕 YENİ: İstasyon Silme (DELETE)
        public async Task<bool> DeleteAsync(string id)
        {
            var result = await _stations.DeleteOneAsync(s => s.Id == id);
            return result.DeletedCount > 0;
            
        }
        
        public async Task ClearAllStationsAsync()
        {
            // Boş bir filtre vererek koleksiyondaki tüm dökümanları sileriz
            await _stations.DeleteManyAsync(Builders<Station>.Filter.Empty);
        }
    }
}
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

        // --- YENİ EKLENEN FİLTRELEME MANTIĞI ---
        public async Task<(List<Station> Data, int TotalCount)> GetFilteredStationsAsync(StationFilterDto filter, int pageNumber, int pageSize)
        {
            var builder = Builders<Station>.Filter;
            var filterDefinition = builder.Empty;

            if (!string.IsNullOrWhiteSpace(filter.StationName))
                filterDefinition &= builder.Regex(s => s.StationName, new BsonRegularExpression(filter.StationName, "i"));
    
            if (!string.IsNullOrWhiteSpace(filter.StationNumber))
                filterDefinition &= builder.Regex(s => s.StationNumber, new BsonRegularExpression(filter.StationNumber, "i"));

            if (!string.IsNullOrWhiteSpace(filter.City)) 
                filterDefinition &= builder.Eq(s => s.City, filter.City);
        
            if (!string.IsNullOrWhiteSpace(filter.District)) 
                filterDefinition &= builder.Eq(s => s.District, filter.District);

            if (!string.IsNullOrWhiteSpace(filter.OperatorName)) 
                filterDefinition &= builder.Regex(s => s.OperatorName, new BsonRegularExpression(filter.OperatorName, "i"));

            if (!string.IsNullOrWhiteSpace(filter.Brand)) 
                filterDefinition &= builder.Regex(s => s.Brand, new BsonRegularExpression(filter.Brand, "i"));

            if (!string.IsNullOrWhiteSpace(filter.ServiceType)) 
                filterDefinition &= builder.Eq(s => s.ServiceType, filter.ServiceType);

            if (!string.IsNullOrWhiteSpace(filter.StationType))
            {
                // Sockets dizisinin içinde en az bir tane tipi (AC veya DC) eşleşen soket var mı diye bakar
                filterDefinition &= builder.ElemMatch(s => s.Sockets, 
                    Builders<Socket>.Filter.Regex(sock => sock.Type, new BsonRegularExpression(filter.StationType, "i")));
            }

            if (filter.IsGreenCharging.HasValue) 
                filterDefinition &= builder.Eq(s => s.IsGreenCharging, filter.IsGreenCharging.Value);
    
            if (filter.IsSmartCharging.HasValue) 
                filterDefinition &= builder.Eq(s => s.IsSmartCharging, filter.IsSmartCharging.Value);
// Sorguyu çalıştır ve sayfala
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
    }
}
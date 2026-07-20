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

        public async Task<IEnumerable<Station>> GetAllAsync()
        {
            return await _stations.Find(_ => true).ToListAsync();
        }

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

            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var searchRegex = new BsonRegularExpression(filter.SearchText, "i");
                var nameFilter = builder.Regex(s => s.StationName, searchRegex);
                var numberFilter = builder.Regex(s => s.StationNumber, searchRegex);
                var brandFilter = builder.Regex(s => s.Brand, searchRegex);
                var operatorFilter = builder.Regex(s => s.OperatorNetwork, searchRegex);

                filterDefinition &= builder.Or(nameFilter, numberFilter, brandFilter, operatorFilter);
            }

            if (!string.IsNullOrWhiteSpace(filter.City))
                filterDefinition &= builder.Regex(s => s.City, new BsonRegularExpression($"^{filter.City}$", "i"));

            if (!string.IsNullOrWhiteSpace(filter.District))
                filterDefinition &= builder.Regex(s => s.District, new BsonRegularExpression(filter.District, "i"));

            if (!string.IsNullOrWhiteSpace(filter.OperatorStation))
                filterDefinition &= builder.Regex(s => s.OperatorStation, new BsonRegularExpression(filter.OperatorStation, "i"));

            if (!string.IsNullOrWhiteSpace(filter.ServiceType))
            {
                var svc = filter.ServiceType.Trim().ToUpperInvariant();
                string pattern;

                if (svc.Contains("PUBLIC") || svc.Contains("HALK"))
                    pattern = "halk|açı|aci|public";
                else if (svc.Contains("PRIVATE") || svc.Contains("ÖZEL") || svc.Contains("OZEL"))
                    pattern = "özel|ozel|private";
                else
                    pattern = System.Text.RegularExpressions.Regex.Escape(filter.ServiceType);

                filterDefinition &= builder.Regex(s => s.ServiceType, new BsonRegularExpression(pattern, "i"));
            }

            if (!string.IsNullOrWhiteSpace(filter.StationType))
            {
                filterDefinition &= builder.ElemMatch(s => s.Sockets,
                    Builders<Socket>.Filter.Regex(sock => sock.Type, new BsonRegularExpression(filter.StationType, "i")));
            }

            var totalCount = (int)await _stations.CountDocumentsAsync(filterDefinition);
            var data = await _stations.Find(filterDefinition)
                .Skip((pageNumber - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return (data, totalCount);
        }

        public async Task InsertManyAsync(List<Station> stations)
        {
            if (stations != null && stations.Any())
            {
                await _stations.InsertManyAsync(stations);
            }
        }

        public async Task AddAsync(Station station)
        {
            await _stations.InsertOneAsync(station);
        }

        public async Task<bool> UpdateAsync(string id, Station station)
        {
            var result = await _stations.ReplaceOneAsync(s => s.Id == id, station);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var result = await _stations.DeleteOneAsync(s => s.Id == id);
            return result.DeletedCount > 0;
        }

        public async Task ClearAllStationsAsync()
        {
            await _stations.DeleteManyAsync(Builders<Station>.Filter.Empty);
        }

        public async Task<List<string>> GetDistinctCitiesAsync()
        {
            var cities = await _stations.DistinctAsync<string>(
                "City",
                Builders<Station>.Filter.Ne(s => s.City, null) & Builders<Station>.Filter.Ne(s => s.City, ""));

            var list = await cities.ToListAsync();
            return list.Where(c => !string.IsNullOrWhiteSpace(c)).OrderBy(c => c).ToList();
        }

        public async Task<List<string>> GetDistrictsByCityAsync(string city)
        {
            var filter = Builders<Station>.Filter.Regex(s => s.City, new BsonRegularExpression($"^{city}$", "i"))
                         & Builders<Station>.Filter.Ne(s => s.District, null)
                         & Builders<Station>.Filter.Ne(s => s.District, "");

            var districts = await _stations.DistinctAsync<string>("District", filter);
            var list = await districts.ToListAsync();
            return list.Where(d => !string.IsNullOrWhiteSpace(d)).OrderBy(d => d).ToList();
        }

        // 🟢 YENİ: Dedup kontrolü için mevcut StationNumber'ları çekiyoruz
        public async Task<HashSet<string>> GetExistingStationNumbersAsync()
        {
            var numbers = await _stations
                .Find(Builders<Station>.Filter.Empty)
                .Project(s => s.StationNumber)
                .ToListAsync();

            return numbers.Where(n => !string.IsNullOrWhiteSpace(n)).ToHashSet();
        }

        // 🟢 YENİ: Aktif batch'leri ve her birindeki kayıt sayısını gruplu döndürür
        public async Task<Dictionary<string, int>> GetBatchCountsAsync()
        {
            var pipeline = _stations.Aggregate()
                .Match(s => s.ImportBatchId != null && s.ImportBatchId != "")
                .Group(s => s.ImportBatchId, g => new { BatchId = g.Key, Count = g.Count() });

            var results = await pipeline.ToListAsync();
            return results
                .Where(r => r.BatchId != null)
                .ToDictionary(r => r.BatchId!, r => r.Count);
        }

        // 🟢 YENİ: Belirli bir tarihe eklenen kayıtları siler (gün bazlı, saat dikkate alınmaz)
        public async Task<int> DeleteByDateAsync(DateTime date)
        {
            var startOfDay = date.Date;
            var endOfDay = startOfDay.AddDays(1);

            var filter = Builders<Station>.Filter.Gte(s => s.AddedDate, startOfDay)
                       & Builders<Station>.Filter.Lt(s => s.AddedDate, endOfDay);

            var result = await _stations.DeleteManyAsync(filter);
            return (int)result.DeletedCount;
        }

        // 🟢 YENİ: Belirli bir import batch'ine ait tüm kayıtları siler
        public async Task<int> DeleteByBatchIdAsync(string batchId)
        {
            var result = await _stations.DeleteManyAsync(s => s.ImportBatchId == batchId);
            return (int)result.DeletedCount;
        }
    }
}
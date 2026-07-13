
using AntalyaStation.API.Models; // Station modelini kullanmak için
using MongoDB.Driver;            // MongoDB kütüphanesi
using Microsoft.Extensions.Options; // Ayarları okumak için

namespace AntalyaStation.API.Repositories
{
    public class MongoStationRepository : IStationRepository
    {
        private readonly IMongoCollection<Station> _stations;

        public MongoStationRepository(IOptions<MongoDbSettings> settings)
        {
            // Veritabanına bağlan
            var client = new MongoClient(settings.Value.ConnectionString);
            var database = client.GetDatabase(settings.Value.DatabaseName);
            // "Stations" koleksiyonuna (tabloya) eriş
            _stations = database.GetCollection<Station>("Stations");
        }

        public async Task<(List<Station> Data, int TotalCount)> GetPagedStationsAsync(int pageNumber, int pageSize)
        {
            // 1. Toplam kaç kayıt var? (Pagination için lazım)
            var totalCount = (int)await _stations.CountDocumentsAsync(_ => true);

            // 2. İlgili sayfayı getir (Skip: atla, Limit: getir)
            var data = await _stations.Find(_ => true)
                .Skip((pageNumber - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return (data, totalCount);
        }
    }
}
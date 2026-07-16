using AntalyaStation.API.DTOs;
using AntalyaStation.API.Models;
using AntalyaStation.API.Repositories;

namespace AntalyaStation.API.Services
{
    public class StationService : IStationService
    {
        private readonly IStationRepository _repository;

        public StationService(IStationRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<Station>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        // 🟢 Artık 'object' değil, doğrudan 'DashboardStatsDto' döndürüyoruz
        public async Task<DashboardStatsDto> GetDashboardStatsAsync()
        {
            var stations = (await _repository.GetAllAsync()).ToList(); 

            var stats = new DashboardStatsDto
            {
                TotalActiveStations = stations.Count(s => s.Status == "Active"),
                TotalPowerCapacity = stations.Sum(s => s.TotalPower),
                TotalSocketCount = stations.Sum(s => s.SocketCount),
                
                // Burada da anonim nesne değil, CompanyPowerDto listesi oluşturuyoruz
                PowerByCompany = stations
                    .GroupBy(s => s.Brand)
                    .Select(g => new CompanyPowerDto 
                    { 
                        CompanyName = g.Key ?? "Bilinmiyor", 
                        TotalPower = g.Sum(s => s.TotalPower) 
                    }).ToList()
            };

            return stats;
        }
    }
}
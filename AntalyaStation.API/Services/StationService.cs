using System.Globalization;
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
            var allSockets = stations.SelectMany(s => s.Sockets).ToList();
            var tr = new CultureInfo("tr-TR");
            
            var stats = new DashboardStatsDto
            {
                TotalActiveStations = stations.Count(s => s.Status == "Active"),
                TotalPowerCapacity = stations.Sum(s => s.TotalPower),
                TotalSocketCount = stations.Sum(s => s.SocketCount),
                
                
                AcSocketCount = allSockets.Count(sock =>
                    string.IsNullOrEmpty(sock.Type) || !sock.Type.ToUpper().Contains("DC")),
                DcSocketCount = allSockets.Count(sock =>
                    !string.IsNullOrEmpty(sock.Type) && sock.Type.ToUpper().Contains("DC")),

                GreenChargingCount = stations.Count(s => s.IsGreenCharging),
                SmartChargingCount = stations.Count(s => s.IsSmartCharging),

                
                
                // Burada da anonim nesne değil, CompanyPowerDto listesi oluşturuyoruz
                PowerByCompany = stations
                    .GroupBy(s => s.Brand)
                    .Select(g => new CompanyPowerDto
                    {
                        CompanyName = g.Key ?? "Bilinmiyor",
                        TotalPower = (decimal)g.Sum(s => s.TotalPower)
                    })
                    .OrderByDescending(c => c.TotalPower)
                    .ToList(),

                PowerByDistrict = stations
                    .GroupBy(s => string.IsNullOrWhiteSpace(s.District) ? "Belirtilmemiş" : s.District)
                    .Select(g => new DistrictMetricDto
                    {
                        DistrictName = g.Key,
                        TotalPower = (decimal)g.Sum(s => s.TotalPower),
                        StationCount = g.Count()
                    })
                    .OrderByDescending(d => d.TotalPower)
                    .ToList(),

                StationsByServiceType = stations
                    .GroupBy(s => string.IsNullOrWhiteSpace(s.ServiceType) ? "Belirtilmemiş" : s.ServiceType)
                    .Select(g => new ServiceTypeMetricDto
                    {
                        ServiceType = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList(),

                MonthlyGrowth = stations
                    .GroupBy(s => new { s.AddedDate.Year, s.AddedDate.Month })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                    .Select(g => new MonthlyGrowthDto
                    {
                        Month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy", tr),
                        NewStations = g.Count()
                    })
                    .TakeLast(12)
                    .ToList()
            };

            return stats;
        }
    }
}
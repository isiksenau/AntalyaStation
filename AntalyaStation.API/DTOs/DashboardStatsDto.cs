namespace AntalyaStation.API.DTOs
{
    public class DashboardStatsDto
    {
        public int TotalActiveStations { get; set; }
        public double TotalPowerCapacity { get; set; }
        public int TotalSocketCount { get; set; }
        
        // Şirket bazlı güç dağılımı (Sadece özet isim ve değer)
        public List<CompanyPowerDto> PowerByCompany { get; set; } = new();
        public List<BrandSocketDto> SocketsByBrand { get; set; } = new();
        
        // 💡 REQUIRED FOR ADVANCED CHARTS PANEL
        public int AcSocketCount { get; set; }
        public int DcSocketCount { get; set; }
        public List<DistrictMetricDto> PowerByDistrict { get; set; } = new();
        
        public int GreenChargingCount { get; set; }
        public int SmartChargingCount { get; set; }
        public List<ServiceTypeMetricDto> StationsByServiceType { get; set; } = new();
        public List<MonthlyGrowthDto> MonthlyGrowth { get; set; } = new();
    }

    public class CompanyPowerDto
    {
        public string CompanyName { get; set; } = string.Empty;
        public decimal TotalPower { get; set; } = 0M;
    }
    public class BrandSocketDto
    {
        public string BrandName { get; set; } = string.Empty;
        public int SocketCount { get; set; }
    }
    public class DistrictMetricDto
    {
        public string DistrictName { get; set; } = string.Empty;
        public decimal? TotalPower { get; set; }
        public int StationCount { get; set; }
    }
    public class ServiceTypeMetricDto
    {
        public string ServiceType { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class MonthlyGrowthDto
    {
        public string Month { get; set; } = string.Empty;
        public int NewStations { get; set; }
    }
}
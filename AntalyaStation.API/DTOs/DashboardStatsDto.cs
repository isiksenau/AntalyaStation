namespace AntalyaStation.API.DTOs
{
    public class DashboardStatsDto
    {
        public int TotalActiveStations { get; set; }
        public double TotalPowerCapacity { get; set; }
        public int TotalSocketCount { get; set; }
        
        // Şirket bazlı güç dağılımı (Sadece özet isim ve değer)
        public List<CompanyPowerDto> PowerByCompany { get; set; } = new();
    }

    public class CompanyPowerDto
    {
        public string CompanyName { get; set; } = string.Empty;
        public double TotalPower { get; set; }
    }
}
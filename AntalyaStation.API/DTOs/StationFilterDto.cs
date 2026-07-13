namespace AntalyaStation.API.DTOs
{
    public class StationFilterDto
    {
        public string? StationName { get; set; }
        public string? StationNumber { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? OperatorName { get; set; } // 🎯 Station modelindeki isimle eşitlendi
        public string? Brand { get; set; }
        public string? ServiceType { get; set; }
        public string? StationType { get; set; } // AC/DC kırılımı için
        
        // null = Fark etmez, true = Evet, false = Hayır
        public bool? IsGreenCharging { get; set; }
        public bool? IsSmartCharging { get; set; }
    }
}
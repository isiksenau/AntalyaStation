namespace AntalyaStation.API.DTOs
{
    public class StationFilterDto
    {
        public string? SearchText { get; set; }
        public string? StationName { get; set; }
        public string? StationNumber { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? OperatorNetwork { get; set; }
        public string? OperatorStation { get; set; }        
        public string? Brand { get; set; }
        public string? ServiceType { get; set; }
        public string? StationType { get; set; } // AC/DC kırılımı için
        
        // null = Fark etmez, true = Evet, false = Hayır
        public bool? IsGreenCharging { get; set; }
        public bool? IsSmartCharging { get; set; }
        public string? SortBy { get; set; }
        public string? SortDirection { get; set; }
        public bool IncludeInactive { get; set; }
    }
}
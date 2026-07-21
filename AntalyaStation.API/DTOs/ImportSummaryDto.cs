namespace AntalyaStation.API.DTOs
{
    public class ImportSummaryDto
    {
        public int TotalRows { get; set; }
        public int InsertedRows { get; set; }
        public int ReactivatedRows { get; set; } // 🟢 Yeni Eklendi: Pasiften aktife alınanlar

        public int SkippedRows { get; set; }
        public string BatchId { get; set; } = string.Empty;
        public List<ImportedStationDto> ImportedStations { get; set; } = new();
    }

    public class ImportedStationDto
    {
        public string Id { get; set; } = string.Empty; // 🟢 Tekil işlem için MongoDB Id'si
        public string StationNumber { get; set; } = string.Empty;
        public string StationName { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public DateTime AddedDate { get; set; }
        public bool IsActive { get; set; } = true; // 🟢 Durum bilgisi
    }

    public class ImportBatchDto
    {
        public string BatchId { get; set; } = string.Empty;
        public int StationCount { get; set; }
        public DateTime UploadedDate { get; set; }
        public bool IsActive { get; set; } = true; // 🟢 Batch genel durumu
        public List<ImportedStationDto> Stations { get; set; } = new();
    }
}
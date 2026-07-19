namespace AntalyaStation.API.DTOs
{
    public class ImportSummaryDto
    {
        public int TotalRows { get; set; }
        public int InsertedRows { get; set; }
        public int SkippedRows { get; set; }
        public string BatchId { get; set; } = string.Empty;
    }
}
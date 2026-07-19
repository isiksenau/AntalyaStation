using Microsoft.AspNetCore.Http;
using AntalyaStation.API.DTOs;

namespace AntalyaStation.API.Services
{
    public interface IExcelImportService
    {
        Task<ImportSummaryDto> ImportStationsFromExcelAsync(IFormFile file);
        Task<Dictionary<string, int>> GetActiveImportBatchesAsync();
        Task<int> PurgeStationsByDateAsync(DateTime targetDate);
        Task<int> PurgeStationsByBatchIdAsync(string batchId);
    }
}
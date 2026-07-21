using AntalyaStation.API.DTOs;
using AntalyaStation.API.Models;

namespace AntalyaStation.API.Repositories
{
    public interface IStationRepository
    {
        Task<List<ImportBatchDto>> GetImportBatchesAsync();
        Task<bool> UpdateStatusAsync(string stationId, bool isActive);
        Task<bool> UpdateBatchStatusAsync(string batchId, bool isActive);
        Task<IEnumerable<Station>> GetAllAsync();
        Task<(List<Station> Data, int TotalCount)> GetPagedStationsAsync(int pageNumber, int pageSize);
        Task<(List<Station> Data, int TotalCount)> GetFilteredStationsAsync(StationFilterDto filter, int pageNumber, int pageSize);
        Task InsertManyAsync(List<Station> stations);
        Task AddAsync(Station station);
        Task<bool> UpdateAsync(string id, Station station);
       // Task<bool> DeleteAsync(string id);
        Task ClearAllStationsAsync();
        Task<bool> DeactivateAsync(string id);

        Task<List<string>> GetDistinctCitiesAsync();
        Task<List<string>> GetDistrictsByCityAsync(string city);
        Task<Dictionary<string, int>> GetBatchCountsAsync();

        // 🟢 YENİ: Excel import / batch yönetimi için
        Task<HashSet<string>> GetExistingStationNumbersAsync();
        Task<int> DeactivateByDateAsync(DateTime date);
        Task<int> DeactivateByBatchIdAsync(string batchId);
        
        Task<List<Station>> GetAllStationsRawAsync(); // Aktif/Pasif ayırt etmeksizin tümünü getirmeli

    }
}
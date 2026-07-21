using AntalyaStation.API.DTOs;
using AntalyaStation.API.Models;

namespace AntalyaStation.API.Repositories
{
    public interface IStationRepository
    {
        Task<IEnumerable<Station>> GetAllAsync();
        Task<(List<Station> Data, int TotalCount)> GetPagedStationsAsync(int pageNumber, int pageSize);
        Task<(List<Station> Data, int TotalCount)> GetFilteredStationsAsync(StationFilterDto filter, int pageNumber, int pageSize);
        Task InsertManyAsync(List<Station> stations);
        Task AddAsync(Station station);
        Task<bool> UpdateAsync(string id, Station station);
        Task<bool> DeleteAsync(string id);
        Task<bool> DeactivateAsync(string id);
        Task ClearAllStationsAsync();

        Task<List<string>> GetDistinctCitiesAsync();
        Task<List<string>> GetDistrictsByCityAsync(string city);

        // 🟢 YENİ: Excel import / batch yönetimi için
        Task<HashSet<string>> GetExistingStationNumbersAsync();
        Task<Dictionary<string, int>> GetBatchCountsAsync();
        Task<int> DeactivateByDateAsync(DateTime date);
        Task<int> DeactivateByBatchIdAsync(string batchId);
    }
}
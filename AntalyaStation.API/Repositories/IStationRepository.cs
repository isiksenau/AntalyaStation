using AntalyaStation.API.DTOs;
using AntalyaStation.API.Models;

namespace AntalyaStation.API.Repositories
{
    public interface IStationRepository
    {
        // 🟢 UYUM: Burası IEnumerable olmalı
        Task<IEnumerable<Station>> GetAllAsync();

        Task<(List<Station> Data, int TotalCount)> GetPagedStationsAsync(int pageNumber, int pageSize);
        Task<(List<Station> Data, int TotalCount)> GetFilteredStationsAsync(StationFilterDto filter, int pageNumber, int pageSize);
        Task InsertManyAsync(List<Station> stations);
        Task AddAsync(Station station);
        Task<bool> UpdateAsync(string id, Station station);
        Task<bool> DeleteAsync(string id);
        Task ClearAllStationsAsync();
    }
}
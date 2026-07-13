using AntalyaStation.API.DTOs;
using AntalyaStation.API.Models;

namespace AntalyaStation.API.Repositories
{
    public interface IStationRepository
    {
        // Temel sayfalama
        Task<(List<Station> Data, int TotalCount)> GetPagedStationsAsync(int pageNumber, int pageSize);
        
        // Gelişmiş filtreleme
        Task<(List<Station> Data, int TotalCount)> GetFilteredStationsAsync(StationFilterDto filter, int pageNumber, int pageSize);
        
        // Toplu ekleme
        Task InsertManyAsync(List<Station> stations);

        // 🆕 YENİ CRUD METOTLARI
        Task AddAsync(Station station);
        Task<bool> UpdateAsync(string id, Station station);
        Task<bool> DeleteAsync(string id);
    }
}
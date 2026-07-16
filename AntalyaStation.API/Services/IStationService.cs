using AntalyaStation.API.DTOs;
using AntalyaStation.API.Models;

namespace AntalyaStation.API.Services
{
    public interface IStationService
    {
        Task<IEnumerable<Station>> GetAllAsync();
        Task<DashboardStatsDto> GetDashboardStatsAsync();
    }
}
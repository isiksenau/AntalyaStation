using AntalyaStation.API.Models; // Station modelini kullanabilmek için ekledik

namespace AntalyaStation.API.Repositories
{
    public interface IStationRepository
    {
        // Sayfalama (Pagination) için veriyi getiren imza
        Task<(List<Station> Data, int TotalCount)> GetPagedStationsAsync(int pageNumber, int pageSize);
    }
}
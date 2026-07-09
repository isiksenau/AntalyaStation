using AntalyaStation.API.Models; // Station modelini kullanabilmek için ekledik

namespace AntalyaStation.API.Repositories
{
    public interface IStationRepository
    {
        Task<(List<Station> Data, int TotalCount)> GetPagedStationsAsync(int pageNumber, int pageSize);// Sayfalama (Pagination) için veriyi getiren imza
        Task InsertManyAsync(List<Station> stations); //Toplu istasyon ekleme
    }
}//(Interface): Yapılabilecek işlemleri listeler (GetById, GetAll, Create, Update, Delete)
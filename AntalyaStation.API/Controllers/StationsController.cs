using Microsoft.AspNetCore.Mvc;
using AntalyaStation.API.Repositories;

namespace AntalyaStation.API.Controllers;

[ApiController]
[Route("api/[controller]")] // Adresi: api/stations olacak
public class StationController : ControllerBase
{//İstasyonları listeleme (GET), silme (DELETE) ve güncelleme (PUT) kapıları.
    private readonly IStationRepository _stationRepository;

    // Veritabanı sorgu katmanını (Repository) Dependency Injection ile içeri alıyoruz
    public StationController(IStationRepository stationRepository)
    {
        _stationRepository = stationRepository;
    }

    /// <summary>
    /// MongoDB'deki istasyonları sayfa numarası ve sayfa boyutuna göre filtreleyerek getirir.
    /// Bu yapı Blazor tarafında performanslı bir listeleme (Pagination) yapmamızı sağlayacak.
    /// </summary>
    [HttpGet] // GET isteği atıldığında çalışır
    public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        // Sayfalama parametrelerinin negatif değerler olmaması için güvenlik kontrolü
        if (pageNumber < 1 || pageSize < 1)
        {
            return BadRequest("Sayfa numarası (pageNumber) ve sayfa boyutu (pageSize) 1'den küçük olamaz.");
        }

        // Veritabanından hem o sayfaya ait verileri hem de toplam kayıt sayısını aynı anda çekiyoruz
        var (stations, totalCount) = await _stationRepository.GetPagedStationsAsync(pageNumber, pageSize);

        // UI katmanının (Blazor) sayfalamayı doğru çizebilmesi için gerekli meta bilgileriyle birlikte dönüyoruz
        return Ok(new
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Data = stations
        });
    }
}
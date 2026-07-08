using Microsoft.AspNetCore.Mvc;
using AntalyaStation.API.Repositories; // Repository'ye erişmek için

namespace AntalyaStation.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Adresi: api/stations olacak
    public class StationsController : ControllerBase
    {
        private readonly IStationRepository _repository;

        public StationsController(IStationRepository repository)
        {
            _repository = repository;
        }

        [HttpGet] // GET isteği atıldığında çalışır
        public async Task<IActionResult> GetStations(int page = 1, int size = 10)
        {
            var result = await _repository.GetPagedStationsAsync(page, size);
            // Veriyi JSON formatında döndür
            return Ok(new { Data = result.Data, TotalCount = result.TotalCount });
        }
    }
}
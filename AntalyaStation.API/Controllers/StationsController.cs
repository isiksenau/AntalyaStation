using System.Globalization;
using AntalyaStation.API.DTOs;
using AntalyaStation.API.Models;
using AntalyaStation.API.Repositories;
using AntalyaStation.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace AntalyaStation.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StationsController : ControllerBase
    {
        private readonly IStationRepository _repository;
        private readonly IStationService _stationService;

        // TEK CONSTRUCTOR: Hem repository hem service buradan enjekte edilir.
        public StationsController(IStationRepository repository, IStationService stationService)
        {
            _repository = repository;
            _stationService = stationService;
        }

        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] StationFilterDto filter, 
            [FromQuery] int pageNumber = 1, 
            [FromQuery] int pageSize = 10)
        {
            var (data, totalCount) = await _repository.GetFilteredStationsAsync(filter, pageNumber, pageSize);
            return Ok(new { Data = data, TotalCount = totalCount });
        }

        [HttpGet("dashboard-stats")]
        public async Task<IActionResult> GetStats()
        {
            var stats = await _stationService.GetDashboardStatsAsync();
            return Ok(stats);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Post([FromBody] Station station)
        {
            if (station == null) return BadRequest("İstasyon verisi boş olamaz.");

            // 🟢 SUNUCU TARAFLI HESAPLAMA (Frontend'e güvenme, kendin hesapla)
            station.SocketCount = station.Sockets?.Count ?? 0;
            station.TotalPower = station.Sockets?.Sum(s => s.Power) ?? 0;
    
            // Tarihi de burada setlemek daha garantidir
            station.AddedDate = DateTime.Now; 
            station.IsNew = true;
            station.Status = "Active";

            await _repository.AddAsync(station);

            // 🟢 CreatedAtAction içine id parametresini eklemek daha temizdir
            return CreatedAtAction(nameof(Get), new { id = station.Id }, station);
        }

        [HttpPut("{id}")]
        [Authorize] 
        public async Task<IActionResult> Put(string id, [FromBody] Station station)
        {
            if (station == null) return BadRequest("Güncellenecek veri geçersiz.");
            station.Id = id; 
            var isUpdated = await _repository.UpdateAsync(id, station);
            if (!isUpdated) return NotFound("Güncellenecek istasyon bulunamadı.");
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize] 
        public async Task<IActionResult> Delete(string id)
        {
            var isDeleted = await _repository.DeleteAsync(id);
            if (!isDeleted) return NotFound("Silinecek istasyon bulunamadı.");
            return Ok(new { Message = "İstasyon başarıyla silindi." });
        }
        [HttpDelete("clear-all")]
        [Authorize] // Güvenlik için, sadece yetkili kişiler silsin
        public async Task<IActionResult> ClearAll()
        {
            await _repository.ClearAllStationsAsync();
            return Ok(new { Message = "Veritabanı başarıyla temizlendi." });
        }
        [HttpPost("cleanup-data")]
        [Authorize]
        public async Task<IActionResult> CleanupData()
        {
            var stations = (await _repository.GetAllAsync()).ToList();
            var textInfo = new CultureInfo("tr-TR", false).TextInfo;
            int cleanedCount = 0;

            foreach (var s in stations)
            {
                bool isChanged = false;

                // 1. İsimleri düzelt
                var newName = textInfo.ToTitleCase(s.StationName.ToLower());
                if (s.StationName != newName) { s.StationName = newName; isChanged = true; }

                // 2. 0 değerlerini kontrol et (Eğer soket var ama güç 0 ise, belki soket verisi hatalıdır)
                // Burada istersen soketleri yeniden hesaplayıp TotalPower'ı güncelleyebilirsin
                var calculatedPower = s.Sockets.Sum(x => x.Power);
                if (s.TotalPower != calculatedPower) { s.TotalPower = calculatedPower; isChanged = true; }

                // 3. Değişiklik varsa güncelle
                if (isChanged)
                {
                    await _repository.UpdateAsync(s.Id, s);
                    cleanedCount++;
                }
            }
            return Ok(new { Message = $"{cleanedCount} adet istasyon verisi temizlendi ve düzenlendi." });
        }
    }
}
using AntalyaStation.API.DTOs;
using AntalyaStation.API.Models;
using AntalyaStation.API.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AntalyaStation.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StationsController : ControllerBase
    {
        private readonly IStationRepository _repository;

        public StationsController(IStationRepository repository)
        {
            _repository = repository;
        }
        
        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] StationFilterDto filter, 
            [FromQuery] int pageNumber = 1, 
            [FromQuery] int pageSize = 10)
        {
            // [FromQuery] sayesinde Blazor'dan gelen tüm filtreler otomatik olarak filter nesnesine dolar.
            var (data, totalCount) = await _repository.GetFilteredStationsAsync(filter, pageNumber, pageSize);

            return Ok(new 
            { 
                Data = data, 
                TotalCount = totalCount 
            });
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Station station)
        {
            if (station == null) return BadRequest("İstasyon verisi boş olamaz.");

            await _repository.AddAsync(station);
            return CreatedAtAction(nameof(Get), new { searchText = station.StationName }, station);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(string id, [FromBody] Station station)
        {
            if (station == null) return BadRequest("Güncellenecek veri geçersiz.");

            var isUpdated = await _repository.UpdateAsync(id, station);
            if (!isUpdated) return NotFound("Güncellenecek istasyon bulunamadı.");

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var isDeleted = await _repository.DeleteAsync(id);
            if (!isDeleted) return NotFound("Silinecek istasyon bulunamadı.");

            return Ok(new { Message = "İstasyon başarıyla silindi." });
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using AntalyaStation.API.Services;
using Microsoft.AspNetCore.Authorization;
using AntalyaStation.API.DTOs;

namespace AntalyaStation.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StationImportController : ControllerBase
    {
        private readonly IExcelImportService _excelImportService;

        public StationImportController(IExcelImportService excelImportService)
        {
            _excelImportService = excelImportService;
        }

        [HttpPost("excel")]
        [Authorize]
        public async Task<IActionResult> ImportExcel(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Lütfen geçerli bir Excel dosyası yükleyin.");

            try
            {
                var summary = await _excelImportService.ImportStationsFromExcelAsync(file);
                return Ok(new { Message = $"{summary.InsertedRows} adet istasyon başarıyla MongoDB'ye aktarıldı!", Summary = summary });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "İşlem sırasında hata oluştu.", Detail = ex.Message });
            }
        }

        [HttpGet("batches")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GetActiveBatches()
        {
            var batches = await _excelImportService.GetActiveImportBatchesAsync();
            return Ok(batches);
        }

        [HttpDelete("purge-by-date")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> PurgeByDate([FromQuery] string date)
        {
            if (!DateTime.TryParse(date, out DateTime parsedDate))
                return BadRequest(new { Message = "Provided date string format could not be verified." });

            var count = await _excelImportService.DeactivateStationsByDateAsync(parsedDate);
            return Ok(new { Message = $"Batch transaction complete. Deactivated {count} entries from matching date constraint." });
        }

        [HttpDelete("purge-by-batch/{batchId}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> PurgeByBatch(string batchId)
        {
            if (string.IsNullOrEmpty(batchId))
                return BadRequest(new { Message = "Target batch tracking identifier context cannot be null." });

            var count = await _excelImportService.DeactivateStationsByBatchIdAsync(batchId);
            return Ok(new { Message = $"Batch group drop successful. Deactivated {count} nodes matching Token Reference: {batchId}." });
        }
    }
}
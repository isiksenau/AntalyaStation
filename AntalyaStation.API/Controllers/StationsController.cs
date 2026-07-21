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
        private readonly IExcelImportService _excelImportService;

        // UNIFIED CONSTRUCTOR: All core repository, telemetry, and integration services are injected here.
        public StationsController(
            IStationRepository repository, 
            IStationService stationService,
            IExcelImportService excelImportService)
        {
            _repository = repository;
            _stationService = stationService;
            _excelImportService = excelImportService;
        }
    
        #region --- Standard Data Queries & Telemetry ---

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

        [HttpGet("cities")]
        public async Task<IActionResult> GetCities()
        {
            var cities = await _repository.GetDistinctCitiesAsync();
            return Ok(cities);
        }

        [HttpGet("cities/{city}/districts")]
        public async Task<IActionResult> GetDistricts(string city)
        {
            var districts = await _repository.GetDistrictsByCityAsync(city);
            return Ok(districts);
        }

        #endregion

        #region --- Individual CRUD Modifications ---

        [HttpPost]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Post([FromBody] Station station)
        {
            if (station == null) return BadRequest("Station data cannot be empty.");

            // SERVER-SIDE CALCULATION: Protect processing integrity from client payload variations
            station.SocketCount = station.Sockets?.Count ?? 0;
            station.TotalPower = station.Sockets?.Sum(s => s.Power) ?? 0;
    
            station.AddedDate = DateTime.Now; 
            station.IsNew = true;
            station.Status = "Active";

            await _repository.AddAsync(station);

            return CreatedAtAction(nameof(Get), new { id = station.Id }, station);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Put(string id, [FromBody] Station station)
        {
            if (station == null) return BadRequest("The target update payload is invalid.");
            station.Id = id;
            
            var isUpdated = await _repository.UpdateAsync(id, station);
            if (!isUpdated) return NotFound("The requested charging station could not be found.");
            
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Delete(string id)
        {
            var isDeleted = await _repository.DeactivateAsync(id);
            if (!isDeleted) return NotFound("The target charging station could not be found.");
            
            return Ok(new { Message = "Station record deactivated successfully." });
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStationStatus(string id, [FromBody] StatusUpdateDto dto)
        {
            var result = await _excelImportService.UpdateStationStatusAsync(id, dto.IsActive);
            if (!result) return NotFound();
            return Ok(new { message = "Station status updated successfully." });
        }

        [HttpPut("batch/{batchId}/status")]
        public async Task<IActionResult> UpdateBatchStatus(string batchId, [FromBody] StatusUpdateDto dto)
        {
            var result = await _excelImportService.UpdateBatchStatusAsync(batchId, dto.IsActive);
            if (!result) return NotFound();
            return Ok(new { message = "Batch status updated successfully." });
        }

        #endregion

        #region --- Enterprise Bulk Operations & Maintenance ---

        [HttpPost("import-excel")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> ImportExcel(IFormFile? file, [FromServices] IExcelImportService excelImportService)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "Please select a valid Excel file to upload." });

            try
            {
                var summary = await excelImportService.ImportStationsFromExcelAsync(file);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An internal error occurred during processing operation.", detail = ex.Message });
            }
        }

        [HttpGet("import-batches")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GetImportBatches()
        {
            // 🟢 DÜZELTME: GetActiveImportBatchesAsync yerine GetImportBatchesAsync çağrılıyor.
            var batches = await _excelImportService.GetImportBatchesAsync();
            return Ok(batches);
        }

        [HttpDelete("purge-by-date")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> PurgeByDate([FromQuery] string date)
        {
            if (!DateTime.TryParse(date, out DateTime parsedDate))
                return BadRequest(new { Message = "Provided date string format could not be verified." });

            int count = await _excelImportService.DeactivateStationsByDateAsync(parsedDate);
            return Ok(new { Message = $"Batch transaction complete. Deactivated {count} entries from matching date constraint." });
        }

        [HttpDelete("purge-by-batch/{batchId}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> PurgeByBatch(string batchId)
        {
            if (string.IsNullOrEmpty(batchId))
                return BadRequest(new { Message = "Target batch tracking identifier context cannot be null." });

            int count = await _excelImportService.DeactivateStationsByBatchIdAsync(batchId);
            return Ok(new { Message = $"Batch group drop successful. Deactivated {count} nodes matching Token Reference: {batchId}." });
        }

        [HttpDelete("clear-all")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> ClearAll()
        {
            await _repository.ClearAllStationsAsync();
            return Ok(new { Message = "Database repository cleared successfully." });
        }

        [HttpPost("cleanup-data")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> CleanupData()
        {
            var stations = (await _repository.GetAllAsync()).ToList();
            var textInfo = new CultureInfo("tr-TR", false).TextInfo;
            int cleanedCount = 0;

            foreach (var s in stations)
            {
                bool isChanged = false;

                var newName = textInfo.ToTitleCase(s.StationName.ToLower());
                if (s.StationName != newName) { s.StationName = newName; isChanged = true; }

                var calculatedPower = s.Sockets.Sum(x => x.Power);
                if (s.TotalPower != calculatedPower) { s.TotalPower = calculatedPower; isChanged = true; }

                if (isChanged)
                {
                    await _repository.UpdateAsync(s.Id, s);
                    cleanedCount++;
                }
            }
            return Ok(new { Message = $"{cleanedCount} station records successfully cleaned and optimized." });
        }

        #endregion
    }
}
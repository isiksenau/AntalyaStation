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
        private readonly IServiceScopeFactory _scopeFactory; // 🆕
        private readonly GeocodingJobState _geoJobState; // 🆕

        // UNIFIED CONSTRUCTOR: All core repository, telemetry, and integration services are injected here.
        public StationsController(
            IStationRepository repository,
            IStationService stationService,
            IExcelImportService excelImportService,
            IServiceScopeFactory scopeFactory, // 🆕
            GeocodingJobState geoJobState) // 🆕
        {
            _repository = repository;
            _stationService = stationService;
            _excelImportService = excelImportService;
            _scopeFactory = scopeFactory; // 🆕
            _geoJobState = geoJobState; // 🆕
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
        [Authorize(Policy = "Permission.DataImport")]
        public async Task<IActionResult> ImportExcel(IFormFile? file,
            [FromServices] IExcelImportService excelImportService)
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
                return StatusCode(500,
                    new { error = "An internal error occurred during processing operation.", detail = ex.Message });
            }
        }

        [HttpGet("import-batches")]
        [Authorize(Policy = "Permission.DataImport")]
        public async Task<IActionResult> GetImportBatches()
        {
            // 🟢 DÜZELTME: GetActiveImportBatchesAsync yerine GetImportBatchesAsync çağrılıyor.
            var batches = await _excelImportService.GetImportBatchesAsync();
            return Ok(batches);
        }

        [HttpDelete("purge-by-date")]
        [Authorize(Policy = "Permission.DataImport")]
        public async Task<IActionResult> PurgeByDate([FromQuery] string date)
        {
            if (!DateTime.TryParse(date, out DateTime parsedDate))
                return BadRequest(new { Message = "Provided date string format could not be verified." });

            int count = await _excelImportService.DeactivateStationsByDateAsync(parsedDate);
            return Ok(new
            {
                Message = $"Batch transaction complete. Deactivated {count} entries from matching date constraint."
            });
        }

        [HttpDelete("purge-by-batch/{batchId}")]
        [Authorize(Policy = "Permission.DataImport")]
        public async Task<IActionResult> PurgeByBatch(string batchId)
        {
            if (string.IsNullOrEmpty(batchId))
                return BadRequest(new { Message = "Target batch tracking identifier context cannot be null." });

            int count = await _excelImportService.DeactivateStationsByBatchIdAsync(batchId);
            return Ok(new
            {
                Message = $"Batch group drop successful. Deactivated {count} nodes matching Token Reference: {batchId}."
            });
        }

        [HttpDelete("clear-all")]
        [Authorize(Policy = "Permission.DataImport")]
        public async Task<IActionResult> ClearAll()
        {
            await _repository.ClearAllStationsAsync();
            return Ok(new { Message = "Database repository cleared successfully." });
        }

        [HttpPost("cleanup-data")]
        [Authorize(Policy = "Permission.SystemConsole")]
        public async Task<IActionResult> CleanupData()
        {
            var stations = (await _repository.GetAllAsync()).ToList();
            var textInfo = new CultureInfo("tr-TR", false).TextInfo;
            int cleanedCount = 0;
            int removedSocketCount = 0;

            foreach (var s in stations)
            {
                bool isChanged = false;

                var newName = textInfo.ToTitleCase(s.StationName.ToLower());
                if (s.StationName != newName)
                {
                    s.StationName = newName;
                    isChanged = true;
                }

                // 🟢 Bozuk Excel importundan kalan "hayalet" soketleri temizle:
                // gerçek soket numaralarında rakam olur (SKT/2731 gibi); 0 kW olup
                // içinde hiç rakam olmayan kayıtlar (örn. "Soket No") başlık satırı kalıntısıdır.
                var validSockets = s.Sockets
                    .Where(sock => sock.SocketNumber.Any(char.IsDigit) || sock.Power > 0)
                    .ToList();

                if (validSockets.Count != s.Sockets.Count)
                {
                    removedSocketCount += s.Sockets.Count - validSockets.Count;
                    s.Sockets = validSockets;
                    s.SocketCount = validSockets.Count;
                    isChanged = true;
                }

                var calculatedPower = s.Sockets.Sum(x => x.Power);
                if (s.TotalPower != calculatedPower) { s.TotalPower = calculatedPower; isChanged = true; }

// 🟢 İlçe (District) düzeltmesi: adres içinde "Akseki" geçen ama ilçesi
// yanlış/boş olan kayıtları düzeltiyor, geriye kalan tüm "Belirtilmemiş"
// olanları da "Merkez" yapıyor.
                var knownDistricts = new List<string>
                {
                    "Akseki", "Aksu", "Alanya", "Döşemealtı", "Elmalı", "Finike", "Gazipaşa", "Gündoğmuş",
                    "İbradı", "Demre", "Kaş", "Kemer", "Kepez", "Konyaaltı", "Korkuteli",
                    "Kumluca", "Manavgat", "Muratpaşa", "Serik"
                };

                if (string.IsNullOrWhiteSpace(s.District) || s.District == "Belirtilmemiş")
                {
                    var matchedDistrict = !string.IsNullOrWhiteSpace(s.Address)
                        ? knownDistricts.FirstOrDefault(d => s.Address.Contains(d, StringComparison.OrdinalIgnoreCase))
                        : null;

                    var newDistrict = matchedDistrict ?? "Merkez";
                    if (s.District != newDistrict) { s.District = newDistrict; isChanged = true; }
                }

                if (isChanged)
                {
                    await _repository.UpdateAsync(s.Id, s);
                    cleanedCount++;
                }
            }

            return Ok(new
            {
                Message =
                    $"{cleanedCount} station records cleaned. {removedSocketCount} invalid socket entries removed."
            });
        }

        #endregion

        [HttpPost("resolve-locations/start")]
        [Authorize(Policy = "Permission.SystemConsole")]
        public IActionResult StartResolveLocations()
        {
            if (_geoJobState.IsRunning)
                return Ok(new { Message = "A geocoding job is already running.", AlreadyRunning = true });

            _geoJobState.IsRunning = true;
            _geoJobState.Resolved = 0;
            _geoJobState.Failed = 0;
            _geoJobState.TotalPending = 0;
            _geoJobState.Remaining = 0;
            _geoJobState.Message = "Starting...";
            _geoJobState.StartedAt = DateTime.Now;
            _geoJobState.FinishedAt = null;

            // 🟢 Bilerek "await" edilmiyor: HTTP isteği anında döner, iş arka planda devam eder.
            // Kendi DI scope'unu oluşturur, böylece bu HTTP isteği bitse bile repository çalışmaya devam edebilir.
            _ = Task.Run(RunGeocodingJobAsync);

            return Ok(new { Message = "Geocoding job started.", AlreadyRunning = false });
        }

        [HttpGet("resolve-locations/status")]
[Authorize(Policy = "Permission.SystemConsole")]
        public IActionResult GetResolveLocationsStatus()
        {
            return Ok(new
            {
                _geoJobState.IsRunning,
                _geoJobState.TotalPending,
                _geoJobState.Resolved,
                _geoJobState.Failed,
                _geoJobState.Remaining,
                _geoJobState.Message
            });
        }

        private async Task RunGeocodingJobAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IStationRepository>();

                var allStations = await repo.GetAllStationsRawAsync();
                var pending = allStations
                    .Where(s => s.Latitude == 0 && s.Longitude == 0 && !string.IsNullOrWhiteSpace(s.Address))
                    .ToList();

                _geoJobState.TotalPending = pending.Count;
                _geoJobState.Remaining = pending.Count;

                if (!pending.Any())
                {
                    _geoJobState.Message = "No stations require geocoding.";
                    return;
                }

                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                client.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "AntalyaStationApp/1.0 (contact: support@antalyastation.local)");

                foreach (var station in pending)
                {
                    var query = $"{station.Address}, {station.District}, Antalya, Turkey";
                    var url =
                        $"https://nominatim.openstreetmap.org/search?format=json&limit=1&countrycodes=tr&q={Uri.EscapeDataString(query)}";

                    try
                    {
                        var results = await client.GetFromJsonAsync<List<NominatimResult>>(url);
                        var match = results?.FirstOrDefault();

                        if (match != null &&
                            double.TryParse(match.lat, CultureInfo.InvariantCulture, out var lat) &&
                            double.TryParse(match.lon, CultureInfo.InvariantCulture, out var lng))
                        {
                            station.Latitude = lat;
                            station.Longitude = lng;
                            await repo.UpdateAsync(station.Id!, station);
                            _geoJobState.Resolved++;
                        }
                        else
                        {
                            _geoJobState.Failed++;
                        }
                    }
                    catch
                    {
                        _geoJobState.Failed++;
                    }

                    _geoJobState.Remaining = _geoJobState.TotalPending - _geoJobState.Resolved - _geoJobState.Failed;
                    _geoJobState.Message =
                        $"{_geoJobState.Resolved} resolved, {_geoJobState.Failed} failed, {_geoJobState.Remaining} remaining...";

                    // Nominatim kullanım politikası: saniyede en fazla 1 istek
                    await Task.Delay(1100);
                }

                _geoJobState.Message =
                    $"Finished. {_geoJobState.Resolved} station(s) geocoded, {_geoJobState.Failed} could not be resolved.";
            }
            catch (Exception ex)
            {
                _geoJobState.Message = $"Job crashed: {ex.Message}";
            }
            finally
            {
                _geoJobState.IsRunning = false;
                _geoJobState.FinishedAt = DateTime.Now;
            }
        }

        private class NominatimResult
        {
            public string lat { get; set; } = string.Empty;
            public string lon { get; set; } = string.Empty;
        }
    }}
using Microsoft.AspNetCore.Mvc;
using AntalyaStation.API.Services;
using Microsoft.AspNetCore.Authorization;
namespace AntalyaStation.API.Controllers;

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
        // 1. Dosya Kontrolü
        if (file == null || file.Length == 0)
            return BadRequest("Lütfen geçerli bir Excel dosyası yükleyin.");

        try
        {
            // 2. Servisi Çağır
            var savedCount = await _excelImportService.ImportStationsFromExcelAsync(file);
            
            return Ok(new { Message = $"{savedCount} adet istasyon başarıyla MongoDB'ye aktarıldı!" });
        }
        catch (Exception ex)
        {
            // 3. 500 Hatası alırsan buradaki hata mesajı sana sebebini söyleyecek (örn: "NullReference" veya "IndexOutRange")
            return StatusCode(500, new { Error = "İşlem sırasında hata oluştu.", Detail = ex.Message });
        }
    }
}
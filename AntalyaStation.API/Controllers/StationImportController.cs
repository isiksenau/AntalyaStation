using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using AntalyaStation.API.Services;

namespace AntalyaStation.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StationImportController : ControllerBase  //Sadece Excel dosyasını yüklemek için (POST) kullanacağımız kapı.
{
    private readonly IExcelImportService _excelImportService;

    // Servis katmanını (Business Logic) Dependency Injection ile içeri alıyoruz
    public StationImportController(IExcelImportService excelImportService)
    {
        _excelImportService = excelImportService;
    }

    /// <summary>
    /// Tarayıcı veya Swagger üzerinden yüklenen Excel dosyasını alır, 
    /// servis katmanına göndererek MongoDB'ye topluca kaydedilmesini sağlar.
    /// </summary>
    [HttpPost("excel")]
    public async Task<IActionResult> ImportExcel(IFormFile file)
    {
        // Dosya varlık ve boşluk kontrolü (Validation)
        if (file == null || file.Length == 0)
        {
            return BadRequest("Lütfen geçerli bir Excel (.xlsx veya .csv) dosyası seçip tekrar deneyin.");
        }

        try
        {
            // Excel dosyasını okuması ve kaydetmesi için servisi tetikliyoruz
            var savedCount = await _excelImportService.ImportStationsFromExcelAsync(file);

            return Ok(new { Message = $"{savedCount} adet istasyon başarıyla MongoDB'ye aktarıldı!" });
        }
        catch (Exception ex)
        {
            // İşlem sırasında oluşabilecek dosya formatı veya veritabanı hatalarını yakalıyoruz
            return StatusCode(500, $"Dosya işlenirken sistemsel bir hata oluştu: {ex.Message}");
        }
    }
}
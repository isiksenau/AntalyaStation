// HTTP üzerinden gelen dosya yükleme (IFormFile) yeteneklerini kullanmak için ekledik.
using Microsoft.AspNetCore.Http;

// Dosyanın projedeki tam konumunu (klasör yolunu) belirtir.
namespace AntalyaStation.API.Services;

// Dışarıya sadece metotların imzasını (adını ve ne beklediğini) sunan arayüzümüz.
public interface IExcelImportService
{
    // Excel dosyasını alıp, arka planda işleyip, kaydedilen istasyon sayısını (int) dönen asenkron metot.
    Task<int> ImportStationsFromExcelAsync(IFormFile file);
}
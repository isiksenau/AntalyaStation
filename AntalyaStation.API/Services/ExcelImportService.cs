using AntalyaStation.API.Models;
using AntalyaStation.API.Repositories;
// Excel dosyalarını satır satır okumamızı sağlayan EPPlus kütüphanesi.
using OfficeOpenXml;
// Swagger veya Tarayıcıdan yüklenen dosyayı kod tarafında karşılamak için kullanılan kütüphane.
using Microsoft.AspNetCore.Http;

namespace AntalyaStation.API.Services;

public class ExcelImportService : IExcelImportService
{//Excel dosyasını satır satır okuyup Station modeline dönüştürür.
    
    private readonly IStationRepository _stationRepository; // Veritabanı işlemlerini tetikleyebilmek için repository nesnemizi private olarak tanımlıyoruz.
    
    public ExcelImportService(IStationRepository stationRepository) // Dependency Injection (Bağımlılık Enjeksiyonu) ile Repository'yi bu sınıfa bağlıyoruz.
    {
        _stationRepository = stationRepository;// Gelen repository nesnesini yukarıdaki private değişkene eşitliyoruz.
    }

    // Excel okuma algoritmasının bizzat döndüğü asenkron metot.
    public async Task<int> ImportStationsFromExcelAsync(IFormFile file)
    {
        var stationList = new List<Station>();
        using var stream = file.OpenReadStream();
        using var package = new ExcelPackage(stream); //package oluşturuyoruz
        var worksheet = package.Workbook.Worksheets[0]; //1.sayfa
        var rowCount = worksheet.Dimension.End.Row;

        Station currentStation = null; //üzerinde çalıştığımız station

        // 1. satır başlıklar (Sıra No, İstasyon No vb.) olduğu için döngüyü 2. satırdan başlatıyoruz.
        for (int row = 2; row <= rowCount; row++)
        {
            var stationNo = worksheet.Cells[row, 2].Value?.ToString()?.Trim();
            var stationName = worksheet.Cells[row, 3].Value?.ToString()?.Trim();
            var brandName = worksheet.Cells[row, 5].Value?.ToString()?.Trim();
            var address = worksheet.Cells[row, 9].Value?.ToString()?.Trim();
            var socketNo = worksheet.Cells[row, 10].Value?.ToString()?.Trim();
            var socketType = worksheet.Cells[row, 11].Value?.ToString()?.Trim();
            var socketPower = worksheet.Cells[row, 13].Value?.ToString()?.Trim();
            
            if (!string.IsNullOrEmpty(stationNo)) // EĞER "İstasyon No" alanı boş değilse, yeni bir istasyon satırına gelmişiz demektir.
            {
                currentStation = new Station // Yeni bir Station nesnesi örnekliyoruz (Instance oluşturuyoruz).
                {
                    StationNumber = stationNo,
                    StationName = stationName ?? "Bilinmeyen İstasyon",
                    Brand = brandName ?? "Bilinmeyen Marka",
                    Address = address ?? "Adres Belirtilmemiş",
                    Sockets = new List<Socket>()
                };
                stationList.Add(currentStation);
            }

            if (currentStation != null && !string.IsNullOrEmpty(socketNo))
            {
                // Mevcut aktif istasyonun içindeki Sockets listesine yeni bir soket ekliyoruz.
                currentStation.Sockets.Add(new Socket
                {
                    SocketNumber = socketNo,
                    Type = socketType ?? "AC",
                    Power = socketPower ?? "0"
                });
            }
        }

        // Döngü tamamen bittikten sonra, eğer listemizde en az bir tane istasyon birikmişse:
        if (stationList.Any())
        {
            // Repository katmanına giderek bu devasa listeyi topluca MongoDB'ye kaydetmesini söylüyoruz.
            await _stationRepository.InsertManyAsync(stationList);
        }

        // İşlem tamamlandığında toplam kaç adet ana istasyon kaydettiğimizi geri döndürüyoruz.
        return stationList.Count;
    }
}
using AntalyaStation.API.Models;
using AntalyaStation.API.Repositories;
using OfficeOpenXml;
using Microsoft.AspNetCore.Http;

namespace AntalyaStation.API.Services;

public class ExcelImportService : IExcelImportService
{
    private readonly IStationRepository _stationRepository;

    public ExcelImportService(IStationRepository stationRepository)
    {
        _stationRepository = stationRepository;
    }

    public async Task<int> ImportStationsFromExcelAsync(IFormFile file)
    {
        var stationList = new List<Station>();
        using var stream = file.OpenReadStream();
        using var package = new ExcelPackage(stream);
        var worksheet = package.Workbook.Worksheets[0];
        var rowCount = worksheet.Dimension.End.Row;

        Station currentStation = null;

        for (int row = 2; row <= rowCount; row++)
        {
            // Sütun indeksleri görseline göre ayarlandı:
            var stationNo = worksheet.Cells[row, 2].Value?.ToString()?.Trim();     // B Sütunu
            var stationName = worksheet.Cells[row, 3].Value?.ToString()?.Trim();   // C Sütunu
            var brandName = worksheet.Cells[row, 5].Value?.ToString()?.Trim();     // E Sütunu
            var address = worksheet.Cells[row, 8].Value?.ToString()?.Trim();       // H Sütunu

            // Soket Bilgileri
            var socketNo = worksheet.Cells[row, 10].Value?.ToString()?.Trim();     // J Sütunu
            var socketType = worksheet.Cells[row, 12].Value?.ToString()?.Trim();   // L Sütunu (Soket Türü: AC_TYPE2)
            var socketPower = worksheet.Cells[row, 13].Value?.ToString()?.Trim();  // M Sütunu (Güç)

            // 1. Yeni bir istasyon satırı mı?
            if (!string.IsNullOrEmpty(stationNo))
            {
                // Adresten Şehir/İlçe çıkarma (Basit mantık)
                // Örn: "... / ANTALYA" bilgisini ayırmak için
                string city = "Antalya"; 
                string district = "Merkez"; // Adresten ayrıştırmak karmaşık olabilir, şimdilik varsayılan atadık.
                
                if (address != null && address.Contains("/"))
                {
                    var parts = address.Split('/');
                    if (parts.Length > 1) city = parts.Last().Trim();
                }

                currentStation = new Station 
                {
                    StationNumber = stationNo,
                    StationName = stationName ?? "Bilinmeyen İstasyon",
                    Brand = brandName ?? "Bilinmeyen Marka",
                    Address = address ?? "Adres Belirtilmemiş",
                    City = city,
                    District = district,
                    IsGreenCharging = false, // Excel'de özel bir "Yeşil Şarj" sütunu görmediğim için false bıraktım
                    IsSmartCharging = false,
                    Sockets = new List<Socket>()
                };
                stationList.Add(currentStation);
            }

            // 2. Soket ekleme
            if (currentStation != null && !string.IsNullOrEmpty(socketNo))
            {
                currentStation.Sockets.Add(new Socket
                {
                    SocketNumber = socketNo,
                    Type = socketType ?? "AC",
                    Power = socketPower ?? "0"
                });
            }
        }

        if (stationList.Any())
        {
            await _stationRepository.InsertManyAsync(stationList);
        }

        return stationList.Count;
    }
}
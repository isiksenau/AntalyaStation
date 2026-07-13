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
            var serviceType = worksheet.Cells[row, 4].Value?.ToString()?.Trim();   // D Sütunu
            var brandName = worksheet.Cells[row, 5].Value?.ToString()?.Trim();     // E Sütunu
            var operatorNetwork = worksheet.Cells[row, 6].Value?.ToString()?.Trim(); //F
            var operatorStation = worksheet.Cells[row, 7].Value?.ToString()?.Trim(); //G
            var address = worksheet.Cells[row, 9].Value?.ToString()?.Trim();       // I Sütunu

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
                    Address = address ?? "Adres Belirtilmemiş", // 🎯 Adres ataması
                    City = city,
                    District = district,
                    IsGreenCharging = false, 
                    IsSmartCharging = false,
                    Sockets = new List<Socket>(),

                    // 🎯 BURASI ÇOK KRİTİK! Bu 3 satırın olduğundan emin olun:
                    ServiceType = serviceType ?? "Halka Açık",
                    OperatorNetwork = operatorNetwork ?? "Belirtilmemiş",
                    OperatorStation = operatorStation ?? "Belirtilmemiş"
                };
                stationList.Add(currentStation);
            }

            // 2. Soket ekleme
            else if (currentStation != null && !string.IsNullOrEmpty(socketNo))
            {
                currentStation.Sockets.Add(new Socket
                {
                    SocketNumber = socketNo,
                    Type = socketType ?? "AC",
                    Power = socketPower ?? "0"
                });
            }
        }
        foreach (var station in stationList)
        {
            station.TotalSockets = station.Sockets.Count;
        }

        if (stationList.Any())
        {
            await _stationRepository.InsertManyAsync(stationList);
        }

        return stationList.Count;
    }
}
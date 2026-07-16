using AntalyaStation.API.Models;
using AntalyaStation.API.Repositories;
using OfficeOpenXml;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using System.Linq;

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
        TextInfo textInfo = new CultureInfo("tr-TR", false).TextInfo;

        for (int row = 2; row <= rowCount; row++)
        {
            var stationNo = worksheet.Cells[row, 2].Value?.ToString()?.Trim();
            var stationName = worksheet.Cells[row, 3].Value?.ToString()?.Trim();
            var serviceType = worksheet.Cells[row, 4].Value?.ToString()?.Trim();
            var brandName = worksheet.Cells[row, 5].Value?.ToString()?.Trim();
            var operatorNetwork = worksheet.Cells[row, 6].Value?.ToString()?.Trim();
            var operatorStation = worksheet.Cells[row, 7].Value?.ToString()?.Trim();
            var address = worksheet.Cells[row, 9].Value?.ToString()?.Trim();

            var socketNo = worksheet.Cells[row, 10].Value?.ToString()?.Trim();
            var socketType = worksheet.Cells[row, 12].Value?.ToString()?.Trim();
            var socketPower = worksheet.Cells[row, 13].Value?.ToString()?.Trim();

            // 1. FİLTRELEME: Çöp verileri atla
            if (string.IsNullOrEmpty(stationNo) && string.IsNullOrEmpty(socketNo)) continue;
            if (stationName != null && stationName.ToLower().Contains("advertise")) continue;

            // 2. YENİ İSTASYON MU?
            if (!string.IsNullOrEmpty(stationNo))
            {
                string city = "Antalya";
                string district = "Merkez";
                if (address != null && address.Contains("/"))
                {
                    var parts = address.Split('/');
                    if (parts.Length > 1) city = parts.Last().Trim();
                }

                currentStation = new Station
                {
                    StationNumber = stationNo,
                    StationName = textInfo.ToTitleCase(stationName?.ToLower() ?? "Bilinmeyen İstasyon"),
                    Brand = textInfo.ToTitleCase(brandName?.ToLower() ?? "Bilinmeyen Marka"),
                    Address = address ?? "Adres Belirtilmemiş",
                    City = city,
                    District = district,
                    ServiceType = serviceType ?? "Halka Açık",
                    OperatorNetwork = operatorNetwork ?? "Belirtilmemiş",
                    OperatorStation = operatorStation ?? "Belirtilmemiş",
                    Sockets = new List<Socket>()
                };
                stationList.Add(currentStation);
            }
            // 3. SOKET Mİ? (Sadece geçerli bir istasyon varsa ekle)
            else if (currentStation != null && !string.IsNullOrEmpty(socketNo))
            {
                double.TryParse(socketPower, out double powerVal);
                currentStation.Sockets.Add(new Socket
                {
                    SocketNumber = socketNo,
                    Type = socketType ?? "AC",
                    Power = powerVal
                });
            }
        }

        // Final hesaplamaları yap
        foreach (var station in stationList)
        {
            station.SocketCount = station.Sockets.Count;
            station.TotalPower = station.Sockets.Sum(s => s.Power);
        }

        if (stationList.Any())
        {
            await _stationRepository.InsertManyAsync(stationList);
        }

        return stationList.Count;
    }
}
using AntalyaStation.API.DTOs;
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

    private static readonly List<string> KnownDistricts = new()
    {
        "Aksu", "Alanya", "Döşemealtı", "Elmalı", "Finike", "Gazipaşa", "Gündoğmuş",
        "İbradı", "Demre", "Kaş", "Kemer", "Kepez", "Konyaaltı", "Korkuteli",
        "Kumluca", "Manavgat", "Muratpaşa", "Serik"
    };

    // 🟢 GÜVENCE: Statik constructor sayesinde bu servis ilk kez kullanılmadan hemen önce
    // lisans garanti altına alınır — Program.cs'deki ayar herhangi bir sebeple çalışmasa/atlansa bile
    // burada ikinci bir güvenlik katmanı olur.
    static ExcelImportService()
    {
        try
        {
            ExcelPackage.License.SetNonCommercialPersonal("AntalyaStation");
        }
        catch
        {
            // Zaten ayarlanmışsa veya bu API sürümünde IsLicenseSet yoksa sessizce geç
        }
    }

    public ExcelImportService(IStationRepository stationRepository)
    {
        _stationRepository = stationRepository;
    }

    public async Task<ImportSummaryDto> ImportStationsFromExcelAsync(IFormFile file)
    {
        var stationList = new List<Station>();
        var batchId = Guid.NewGuid().ToString("N").Substring(0, 10);

        using var stream = file.OpenReadStream();
        using var package = new ExcelPackage(stream);
        var worksheet = package.Workbook.Worksheets[0];
        var rowCount = worksheet.Dimension.End.Row;

        Station? currentStation = null;
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

            if (string.IsNullOrEmpty(stationNo) && string.IsNullOrEmpty(socketNo)) continue;
            if (stationName != null && stationName.ToLower().Contains("advertise")) continue;

            if (!string.IsNullOrEmpty(stationNo))
            {
                var (city, district) = ExtractCityAndDistrict(address);

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
                    Sockets = new List<Socket>(),
                    ImportBatchId = batchId,
                    Status = "Active", // 🟢 Eklendi
                    IsActive = true   // 🟢 Eklendi
                };

                if (!string.IsNullOrEmpty(socketNo))
                {
                    double.TryParse(socketPower, out double powerVal);
                    currentStation.Sockets.Add(new Socket
                    {
                        SocketNumber = socketNo,
                        Type = socketType ?? "AC",
                        Power = powerVal
                    });
                }

                stationList.Add(currentStation);
            }
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

        foreach (var station in stationList)
        {
            station.SocketCount = station.Sockets.Count;
            station.TotalPower = station.Sockets.Sum(s => s.Power);
        }

       // 🟢 KURUMSAL MANTIK: Mükerrer Kontrolü ve Pasif İstasyonu Aktife Çekme
        var allExistingStations = await _stationRepository.GetAllStationsRawAsync(); // Veritabanındaki tüm İstasyonlar
        
        var insertedStations = new List<Station>();
        int reactivatedCount = 0;
        int skippedCount = 0;

        foreach (var station in stationList)
        {
            var existing = allExistingStations.FirstOrDefault(x => x.StationNumber == station.StationNumber);
            if (existing != null)
            {
                if (!existing.IsActive)
                {
                    // Pasifteydi, tekrar yüklenince aktife çekiliyor
                    await _stationRepository.UpdateStatusAsync(existing.Id, true);
                    reactivatedCount++;
                }
                else
                {
                    // Zaten aktifti, atlanıyor
                    skippedCount++;
                }
            }
            else
            {
                // Hiç yoktu, yeni ekleniyor
                insertedStations.Add(station);
            }
        }

        if (insertedStations.Any())
        {
            await _stationRepository.InsertManyAsync(insertedStations);
        }

        return new ImportSummaryDto
        {
            TotalRows = stationList.Count,
            InsertedRows = insertedStations.Count,
            ReactivatedRows = reactivatedCount,
            SkippedRows = skippedCount,
            BatchId = batchId,
            ImportedStations = insertedStations.Select(s => new ImportedStationDto
            {
                Id = s.Id,
                StationNumber = s.StationNumber,
                StationName = s.StationName,
                Brand = s.Brand,
                AddedDate = s.AddedDate,
                IsActive = s.IsActive
            }).ToList()
        };
    }

    public async Task<List<ImportBatchDto>> GetImportBatchesAsync()
    {
        return await _stationRepository.GetImportBatchesAsync();
    }

    public async Task<bool> UpdateStationStatusAsync(string stationId, bool isActive)
    {
        return await _stationRepository.UpdateStatusAsync(stationId, isActive);
    }

    public async Task<bool> UpdateBatchStatusAsync(string batchId, bool isActive)
    {
        return await _stationRepository.UpdateBatchStatusAsync(batchId, isActive);
    }

    public async Task<Dictionary<string, int>> GetActiveImportBatchesAsync() => await _stationRepository.GetBatchCountsAsync();
    public async Task<int> DeactivateStationsByDateAsync(DateTime date) => await _stationRepository.DeactivateByDateAsync(date);
    public async Task<int> DeactivateStationsByBatchIdAsync(string batchId) => await _stationRepository.DeactivateByBatchIdAsync(batchId);

    private (string City, string District) ExtractCityAndDistrict(string? address)
    {
        string city = "Belirtilmemiş";
        string district = "Belirtilmemiş";

        if (string.IsNullOrWhiteSpace(address)) return (city, district);

        if (address.Contains("/"))
        {
            var parts = address.Split('/');
            city = parts.Last().Trim();
            var beforeCity = string.Join("/", parts.Take(parts.Length - 1));
            var matchedDistrict = KnownDistricts.FirstOrDefault(d => beforeCity.Contains(d, StringComparison.OrdinalIgnoreCase));
            if (matchedDistrict != null) district = matchedDistrict;
        }

        if (district == "Belirtilmemiş")
        {
            var matchedDistrict = KnownDistricts.FirstOrDefault(d => address.Contains(d, StringComparison.OrdinalIgnoreCase));
            if (matchedDistrict != null) district = matchedDistrict;
        }

        return (city, district);
    }
}
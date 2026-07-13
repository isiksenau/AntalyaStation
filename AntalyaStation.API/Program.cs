using AntalyaStation.API.Data;
using AntalyaStation.API.Repositories;
using AntalyaStation.API.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;
var builder = WebApplication.CreateBuilder(args);
ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
// ==========================================
// 1. AYARLAR VE DIŞ KÜTÜPHANE YAPILANDIRMALARI
// ==========================================
// Veritabanı bağlantı ayarlarını appsettings.json'dan okuyoruz.
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));

// EPPlus Excel kütüphanesinin lisans ayarını yapıyoruz.
OfficeOpenXml.ExcelPackage.License.SetNonCommercialPersonal("isu");

// ==========================================
// 2. BAĞIMLILIKLARIN ENJEKTE EDİLMESİ (Dependency Injection)
// ==========================================
// Veritabanı sorgularını yürütecek olan Repository katmanımızı sisteme tanıtıyoruz.
builder.Services.AddScoped<IStationRepository, MongoStationRepository>();

// Excel dosyalarını okuyacak olan akıllı Servis katmanımızı sisteme tanıtıyoruz (Az önce eksik olan yer!).
builder.Services.AddScoped<IExcelImportService, ExcelImportService>();

// API Controller (Dış kapı) mimarisini projeye dahil ediyoruz.
builder.Services.AddControllers();

// Swagger (Arayüz test ekranı) motorlarını sisteme ekliyoruz.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ==========================================
// 3. UYGULAMANIN İNŞA EDİLMESİ (BUILD)
// ==========================================
// Bu satırdan sonra yukarıdaki "builder.Services" alanına hiçbir şey eklenemez!
var app = builder.Build();

// ==========================================
// 4. HTTP REQUEST PIPELINE (MIDDLEWARES)
// ==========================================
// Geliştirme ortamındaysak Swagger arayüzünü tarayıcıya açıyoruz.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Güvenlik, HTTPS yönlendirmesi ve Controller haritalama işlemleri.
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Projeyi resmen ayağa kaldırıp dinlemeye başlıyoruz.
app.Run();
using AntalyaStation.API.Models; 
using AntalyaStation.API.Repositories;

var builder = WebApplication.CreateBuilder(args);

// 1. Ayarları appsettings.json'dan okuyup MongoDbSettings sınıfına bağla
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));

// 2. Repository'yi sisteme tanıt (Dependency Injection)
builder.Services.AddScoped<IStationRepository, MongoStationRepository>();

// 3. Controller'ları (api endpoint'lerini) ekle
builder.Services.AddControllers();

// 4. OpenAPI (Swagger) desteği (İstersen kalsın, istemezsen silebilirsin)
builder.Services.AddOpenApi();

var app = builder.Build();

// 5. Geliştirme ortamındaysan OpenAPI arayüzünü göster
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// 6. Controller'ları yönlendir
app.MapControllers();

app.Run();
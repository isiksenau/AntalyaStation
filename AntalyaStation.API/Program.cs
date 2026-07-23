using AntalyaStation.API.Data;
using AntalyaStation.API.Repositories;
using AntalyaStation.API.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using AntalyaStation.API;   // BearerSecuritySchemeTransformer burada
using Scalar.AspNetCore;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);

ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

// ==========================================================
// 1. AYARLAR
// ==========================================================
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));
//OfficeOpenXml.ExcelPackage.License.SetNonCommercialPersonal("isu");
OfficeOpenXml.ExcelPackage.License.SetNonCommercialPersonal("AntalyaStation");

// ==========================================================
// 2. DI
// ==========================================================
builder.Services.AddScoped<IExcelImportService, ExcelImportService>();
builder.Services.AddScoped<IStationService, StationService>();
builder.Services.AddScoped<IStationRepository, MongoStationRepository>();
builder.Services.AddControllers();

builder.Services.AddScoped<IUserRepository, MongoUserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<AntalyaStation.API.Services.GeocodingJobState>();

// 🔑 Native OpenAPI + JWT şeması (TEK KEZ, class üzerinden)
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]
                    ?? "CokGizliVeUzunBirVarsayilanKeyBurayaGelmeli123!")
            )
        };
    });

// 🟢 Her izin key'i için otomatik bir policy üret: "Permission.ViewMaps" gibi
builder.Services.AddAuthorization(options =>
{
    var allKeys = AntalyaStation.API.Models.PermissionCatalog.All
        .Select(p => p.Key)
        .Append("ManagePermissions"); // SuperAdmin'e özel, katalogda yok ama policy olarak lazım

    foreach (var key in allKeys)
    {
        options.AddPolicy($"Permission.{key}", policy =>
            policy.RequireAssertion(ctx =>
                ctx.User.IsInRole("SuperAdmin") ||            // SuperAdmin her zaman geçer
                ctx.User.HasClaim("permission", key)));       // veya ilgili permission claim'i varsa
    }
});

// 💡 CORS servisini ekliyoruz (builder.Build() satırının üstünde olmalı!)
builder.Services.AddCors();


// ==========================================================
// 3. BUILD
// ==========================================================
var app = builder.Build();



if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// 💡 Blazor'dan gelen tüm istek kapılarını ardına kadar açıyoruz:
app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod());

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
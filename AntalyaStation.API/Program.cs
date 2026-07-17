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

var builder = WebApplication.CreateBuilder(args);

ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

// ==========================================================
// 1. AYARLAR
// ==========================================================
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));
OfficeOpenXml.ExcelPackage.License.SetNonCommercialPersonal("isu");

// ==========================================================
// 2. DI
// ==========================================================
builder.Services.AddScoped<IExcelImportService, ExcelImportService>();
builder.Services.AddScoped<IStationService, StationService>();
builder.Services.AddScoped<IStationRepository, MongoStationRepository>();
builder.Services.AddControllers();


// 🔑 Native OpenAPI + JWT şeması (TEK KEZ, class üzerinden)
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

// 🔑 JWT Authentication (TEK KEZ)
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

builder.Services.AddAuthorization();

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
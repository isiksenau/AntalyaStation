using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection; // 💡 GetRequiredService için gerekli
using System.Net.Http; // 💡 IHttpClientFactory ve HttpClient için gerekli
using AntalyaStation.Client.Handlers;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// 1. Handler'ı kaydediyoruz
builder.Services.AddTransient<AuthenticationHeaderHandler>();

// 2. API için HttpClient'ı handler ile birlikte yapılandırıyoruz
builder.Services.AddHttpClient("AntalyaStationAPI", client => 
    {
        client.BaseAddress = new Uri("http://localhost:5079/"); 
    })
    .AddHttpMessageHandler<AuthenticationHeaderHandler>();

// 3. 💡 TİPİ AÇIKÇA BELİRTTİK (<HttpClient>): Böylece derleyici hata vermeyecek
builder.Services.AddScoped<HttpClient>(sp => 
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("AntalyaStationAPI"));

// 4. Kimlik doğrulama servislerini ekliyoruz
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddCascadingAuthenticationState();

await builder.Build().RunAsync();
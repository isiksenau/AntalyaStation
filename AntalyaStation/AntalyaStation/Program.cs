using AntalyaStation.Client.Handlers;
using AntalyaStation.Client.Pages;
using AntalyaStation.Components;
using AntalyaStation.Client.Handlers;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// --- 🎯 İŞTE DÜZELTMEMİZ GEREKEN KISIM BURASI ---
// HttpClient'ı burada BaseAddress ile kaydediyoruz. 
// Port numaran 5079 ise bu şekilde kalmalı, değilse API'nin portuyla değiştir.
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri("http://localhost:5079") 
});
// ------------------------------------------------

// Blazor Bootstrap servisini ekliyoruz
builder.Services.AddBlazorBootstrap();

builder.Services.AddCascadingAuthenticationState();
// 🔑 Kendi custom provider'ımızı kaydediyoruz — varsayılanın üzerine yazar
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(AntalyaStation.Client._Imports).Assembly);

app.Run();
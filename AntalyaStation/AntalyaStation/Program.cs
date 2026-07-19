using System;
using System.Net.Http;
using System.Net.Http.Headers;
using AntalyaStation.Client.Handlers;
using AntalyaStation.Client.Pages;
using AntalyaStation.Components;
using ApexCharts;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddApexCharts();

// 🟢 DÜZELTME: HttpClient artık API'nin gerçek adresine (5079) işaret ediyor,
// Blazor'un kendi adresine (5015) değil.
builder.Services.AddScoped<AuthenticationHeaderHandler>();

builder.Services.AddHttpClient("AntalyaStation.API", client =>
    {
        client.BaseAddress = new Uri("http://localhost:5079/");
    })
    .AddHttpMessageHandler<AuthenticationHeaderHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>()
    .CreateClient("AntalyaStation.API"));

builder.Services.AddBlazorBootstrap();
builder.Services.AddCascadingAuthenticationState();
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

app.UseStaticFiles();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(AntalyaStation.Client._Imports).Assembly);

app.Run();
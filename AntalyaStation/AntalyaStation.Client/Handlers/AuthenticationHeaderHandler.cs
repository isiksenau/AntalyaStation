using System.Net.Http.Headers;
using Microsoft.JSInterop;

namespace AntalyaStation.Client.Handlers
{
    public class AuthenticationHeaderHandler : DelegatingHandler
    {
        private readonly IJSRuntime _jsRuntime;

        public AuthenticationHeaderHandler(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Tarayıcı hafızasından token'ı okuyoruz
            var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");

            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
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
            try
            {
                var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");

                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            }
            catch (InvalidOperationException)
            {
                // 🟢 DÜZELTME: JS interop henüz hazır değilse (prerender aşaması,
                // circuit henüz kurulmamış vb.) sessizce devam et — token'sız istek
                // gönderilir, bu login isteği için zaten normaldir.
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace AntalyaStation.API;

public sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (authenticationSchemes.Any(authScheme => authScheme.Name == "Bearer"))
        {
            // 💡 1. ÇÖZÜM: Değer türünü IOpenApiSecurityScheme yaparak arayüz uyuşmazlığını giderdik:
            var requirements = new Dictionary<string, IOpenApiSecurityScheme>
            {
                ["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    In = ParameterLocation.Header,
                    BearerFormat = "Json Web Token"
                }
            };
            
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = requirements;

            // 💡 2. ve 3. ÇÖZÜM: 
            // - OpenApiSecuritySchemeReference sınıfını doğrudan çağırdık (Böylece Reference hatası çözüldü).
            // - Değer kısmına Array.Empty<string>() yerine new List<string>() yazdık.
            document.Security = new List<OpenApiSecurityRequirement>
            {
                new()
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
                }
            };
        }
    }
}
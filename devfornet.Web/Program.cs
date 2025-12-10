using devfornet.Web;
using devfornet.Web.Components;
using Ganss.Xss;
using Microsoft.FluentUI.AspNetCore.Components;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddFluentUIComponents();

builder.Services.AddOutputCache();

builder.Services.AddSingleton<IHtmlSanitizer, HtmlSanitizer>();

builder.Services.AddHttpClient<StaticContentClient>(
    "StaticContentClient",
    client =>
    {
        client.BaseAddress = new Uri(
            Environment.GetEnvironmentVariable("DEVFORNET_URI") ?? "https://devfor.net"
        );
    }
);

builder.Services.AddHttpClient<ApiContentClient>(
    "ApiContentClient",
    client =>
    {
        var apiBaseUrl =
            Environment.GetEnvironmentVariable("API_BASE_URL")
            ?? builder.Configuration["ApiService:BaseUrl"]
            ?? "https+http://apiservice"; // Aspire fallback for local dev
        client.BaseAddress = new Uri(apiBaseUrl);
    }
);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseHttpsRedirection();
}

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

await app.RunAsync();

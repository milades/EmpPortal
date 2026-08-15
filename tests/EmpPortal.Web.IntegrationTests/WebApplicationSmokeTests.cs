using System.Net;
using EmpPortal.Application.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EmpPortal.Web.IntegrationTests;

public sealed class WebApplicationSmokeTests : IClassFixture<PortalWebApplicationFactory>
{
    private readonly PortalWebApplicationFactory _factory;

    public WebApplicationSmokeTests(PortalWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task LiveHealthEndpointReturnsHealthyAndSecurityHeaders()
    {
        using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using HttpResponseMessage response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.True(response.Headers.Contains("X-Correlation-ID"));
    }

    [Fact]
    public async Task LoginPageIsStaticSsrAndContainsBothAuthenticationMethods()
    {
        using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using HttpResponseMessage response = await client.GetAsync("/account/login");
        string html = await response.Content.ReadAsStringAsync();
        string decodedHtml = WebUtility.HtmlDecode(html);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("ورود با حساب ویندوز", html, StringComparison.Ordinal);
        Assert.Contains("manual-login", html, StringComparison.Ordinal);
        Assert.Contains("__RequestVerificationToken", html, StringComparison.Ordinal);
        Assert.Contains("متن پایانی آزمایشی", decodedHtml, StringComparison.Ordinal);
        Assert.Contains("در حال اتصال مجدد به پرتال", html, StringComparison.Ordinal);
        Assert.Contains("تلاش مجدد", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Rejoining the server", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SessionStateEndpointRejectsAnonymousBrowser()
    {
        using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using HttpResponseMessage response = await client.GetAsync("/api/auth/session-state");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString(), StringComparison.Ordinal);
    }
}

public sealed class PortalWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PortalDatabase"] =
                    "Server=127.0.0.1,1;Database=EmpPortal_Test;User Id=test;Password=test;Encrypt=False;Connect Timeout=1"
            }));
        builder.ConfigureServices(services =>
            services.AddScoped<IRuntimeSettingsService, TestRuntimeSettingsService>());
    }

    private sealed class TestRuntimeSettingsService : IRuntimeSettingsService
    {
        public Task<IReadOnlyList<RuntimeSettingItem>> GetAllAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RuntimeSettingItem>>([]);

        public Task<string?> GetValueAsync(
            string key,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(key == PortalRuntimeSettingKeys.LoginFooterText
                ? "متن پایانی آزمایشی"
                : null);

        public Task UpdateAsync(
            string key,
            string value,
            Guid actorUserId,
            string actorUpn,
            string correlationId,
            string? ipAddress,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}

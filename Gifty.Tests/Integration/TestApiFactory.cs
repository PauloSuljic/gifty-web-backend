using System.Net.Http.Headers;
using gifty_web_backend;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Gifty.Infrastructure;
using Gifty.Api.Utils;

namespace Gifty.Tests.Integration
{
    public class TestApiFactory : WebApplicationFactory<StartupWrapper>
    {
        // 👇 Shared database name
        private const string InMemoryDbName = "SharedTestDb";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "UseTestAuth", "true" }
                });
            });

            builder.ConfigureServices(services =>
            {
                // ✅ Use Test Auth handler
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

                services.PostConfigure<AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                });

                // ✅ Replace default DbContext with shared in-memory
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<GiftyDbContext>));

                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<GiftyDbContext>(options =>
                {
                    options.UseInMemoryDatabase(InMemoryDbName);
                });
            });
        }

        public HttpClient CreateClientWithTestAuth(string userId)
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", userId);
            return client;
        }
    }
}

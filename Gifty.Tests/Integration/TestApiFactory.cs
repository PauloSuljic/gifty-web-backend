using System.Net.Http.Headers;
using System.Reflection;
using gifty_web_backend;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Gifty.Infrastructure;
using Gifty.Api.Utils;

namespace Gifty.Tests.Integration
{
    public class TestApiFactory : WebApplicationFactory<StartupWrapper>
    {
        private const string InMemoryDbName = "SharedTestDb";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // ✅ Set TEST environment and inject UseTestAuth
            builder.UseEnvironment("Testing");

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

                // ✅ Replace real DB with shared in-memory DB
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

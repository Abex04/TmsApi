using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TmsApi.Data;

namespace TmsApi.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // 1. Supply required test configuration (JWT secret, etc.) - the
        // test host has no user-secrets, so without this,
        // AddJwtBearer/TokenService would throw on missing config.
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "ThisIsASecretKeyForTestingPurposesOnly123456!",
                ["Jwt:Issuer"] = "TmsTestIssuer",
                ["Jwt:Audience"] = "TmsTestAudience",
                ["Jwt:ExpiryMinutes"] = "15"
            });
        });

        // 2. Remove production DbContext and register InMemory with an
        // isolated internal provider - the test host has no connection
        // string, and we don't want tests touching the real Postgres DB.
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<TmsDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<TmsDbContext>();

            var inMemoryProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            services.AddDbContext<TmsDbContext>(options =>
            {
                options.UseInMemoryDatabase("TmsTestDb");
                options.UseInternalServiceProvider(inMemoryProvider);
            });
        });
    }
}

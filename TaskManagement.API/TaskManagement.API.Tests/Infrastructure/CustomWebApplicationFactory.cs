using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.API.Tests.Infrastructure;

public class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    private readonly string _databaseName =
        $"TaskManagementTests_{Guid.NewGuid():N}";

    private const string TestJwtKey =
        "VGhpcy1pcy1hLXRlc3Qta2V5LWZvci1KV1QtYXV0aC0yMDI2LXNlY3VyZQ==";

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("Jwt:Issuer", "TaskManagement.Tests");
        builder.UseSetting("Jwt:Audience", "TaskManagement.Tests.Client");
        builder.UseSetting("Jwt:Key", TestJwtKey);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.RemoveAll<AppDbContext>();

            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = _databaseName,
                Mode = SqliteOpenMode.Memory,
                Cache = SqliteCacheMode.Shared
            }.ToString();

            // Keep the in-memory database alive throughout the test.
            _connection = new SqliteConnection(connectionString);
            _connection.Open();

            // Each DbContext uses its own database connection.
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlite(connectionString);
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection?.Dispose();
        }

        base.Dispose(disposing);
    }
}
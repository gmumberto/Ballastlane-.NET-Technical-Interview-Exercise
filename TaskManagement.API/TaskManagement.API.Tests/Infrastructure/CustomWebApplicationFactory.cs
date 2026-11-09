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

    private const string TestJwtKey =
        "VGhpcy1pcy1hLXRlc3Qta2V5LWZvci1KV1QtYXV0aC0yMDI2LXNlY3VyZQ==";

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting(
            "Jwt:Issuer",
            "TaskManagement.Tests");

        builder.UseSetting(
            "Jwt:Audience",
            "TaskManagement.Tests.Client");

        builder.UseSetting(
            "Jwt:Key",
            TestJwtKey);

        builder.ConfigureTestServices(services =>
        {
            // Remove the original SQL Server DbContext registration.
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.RemoveAll<AppDbContext>();

            // Create a dedicated in-memory SQLite connection.
            _connection = new SqliteConnection(
                "Data Source=:memory:");

            _connection.Open();

            // Register AppDbContext using SQLite only.
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlite(_connection);
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
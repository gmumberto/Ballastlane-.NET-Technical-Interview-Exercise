
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Domain.Entities;
using TaskManagement.Infrastructure.Persistence;
using DomainTask = TaskManagement.Domain.Entities.Task;

namespace TaskManagement.Bootstrap;

public static class DevelopmentSeeder
{
    public static async System.Threading.Tasks.Task SeedDevelopmentAsync(
        IServiceProvider provider,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue<bool>("Demo:Enabled"))
            return;

        var email = configuration["Demo:Email"]
            ?? "demo@example.com";

        var password = configuration["Demo:Password"];

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Demo:Password must be configured when demo seeding is enabled.");
        }

        var passwordError =
            TaskManagement.Application.Common.PasswordPolicy.Validate(
                password);

        if (passwordError is not null)
        {
            throw new InvalidOperationException(
                "Demo:Password must meet the configured password policy.");
        }

        using var scope = provider.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var hasher = scope.ServiceProvider
            .GetRequiredService<IPasswordHasher<User>>();

        await db.Database.MigrateAsync(cancellationToken);

        var userExists = await db.Users.AnyAsync(
            x => x.Email == email,
            cancellationToken);

        if (userExists)
            return;

        var user = new User(
            "Demo User",
            email,
            "PENDING_HASH");

        var passwordHash = hasher.HashPassword(
            user,
            password);

        user.SetPasswordHash(passwordHash);

        db.Users.Add(user);

        db.Tasks.Add(
            new DomainTask(
                "Explore Swagger",
                "Try authenticated task endpoints.",
                DateTime.UtcNow.AddDays(3),
                user.Id));

        db.Tasks.Add(
            new DomainTask(
                "Review architecture",
                "Explore the Clean Architecture projects.",
                DateTime.UtcNow.AddDays(7),
                user.Id));

        await db.SaveChangesAsync(cancellationToken);
    }
}

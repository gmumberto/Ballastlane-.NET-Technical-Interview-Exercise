using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Interfaces;
using User = TaskManagement.Domain.Entities.User;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();

        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                user => user.Email.ToUpper() == normalizedEmail,
                cancellationToken);
    }

    public async Task<User?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                user => user.Id == id,
                cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();

        return await _context.Users
            .AnyAsync(
                user => user.Email.ToUpper() == normalizedEmail,
                cancellationToken);
    }

    public async Task<bool> TryAddAsync(User user, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Users.AddAsync(user, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (
            (ex.InnerException is SqlException sql && (sql.Number == 2601 || sql.Number == 2627)) ||
            (ex.InnerException is SqliteException sqlite && sqlite.SqliteErrorCode == 19 &&
                (sqlite.SqliteExtendedErrorCode == 2067 || sqlite.SqliteExtendedErrorCode == 1555)))
        {
            _context.Entry(user).State = EntityState.Detached;
            return false;
        }
    }
}

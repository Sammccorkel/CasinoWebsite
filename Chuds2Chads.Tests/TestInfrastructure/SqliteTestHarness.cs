using Chuds2Chads.Data;
using Chuds2Chads.Data.Entities;
using Chuds2Chads.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Chuds2Chads.Tests.TestInfrastructure;

public sealed class SqliteTestHarness : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteTestHarness()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        services.AddScoped<WalletService>();
        services.AddScoped<LeaderboardService>();
        services.AddSingleton<MultiplayerService>();

        ServiceProvider = services.BuildServiceProvider();

        using var scope = ServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
    }

    public ServiceProvider ServiceProvider { get; }

    public async Task<ApplicationUser> CreateUserAsync(string username, long? walletBalance = 1_000, string? friendCode = null)
    {
        await using var scope = ServiceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = username,
            NormalizedUserName = username.ToUpperInvariant(),
            Email = $"{username}@example.com",
            NormalizedEmail = $"{username}@example.com".ToUpperInvariant(),
            FriendCode = friendCode ?? GenerateFriendCode(username),
            CreatedDate = DateTime.UtcNow
        };

        db.Users.Add(user);

        if (walletBalance.HasValue)
        {
            db.Wallets.Add(new Wallet
            {
                UserId = user.Id,
                Balance = walletBalance.Value,
                UpdatedUtc = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
        return user;
    }

    public async Task AddFriendshipAsync(Guid userId, Guid friendUserId)
    {
        await using var scope = ServiceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (!await db.Friendships.AnyAsync(f => f.UserId == userId && f.FriendUserId == friendUserId))
        {
            db.Friendships.Add(new Friendship { UserId = userId, FriendUserId = friendUserId });
        }

        if (!await db.Friendships.AnyAsync(f => f.UserId == friendUserId && f.FriendUserId == userId))
        {
            db.Friendships.Add(new Friendship { UserId = friendUserId, FriendUserId = userId });
        }

        await db.SaveChangesAsync();
    }

    public T GetRequiredService<T>() where T : notnull => ServiceProvider.GetRequiredService<T>();

    public void Dispose()
    {
        ServiceProvider.Dispose();
        _connection.Dispose();
    }

    private static string GenerateFriendCode(string username)
    {
        var seed = $"{username}-{Guid.NewGuid():N}".ToUpperInvariant();
        return $"C2C-{seed[..4]}-{seed[4..8]}";
    }
}

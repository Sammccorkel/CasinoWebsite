using Chuds2Chads.Services;
using Chuds2Chads.Tests.TestInfrastructure;
using Xunit;

namespace Chuds2Chads.Tests.Services;

public class LeaderboardServiceTests : IDisposable
{
    private readonly SqliteTestHarness _harness = new();

    [Fact]
    public async Task GetLeaderboardAsync_OrdersPlayersByChipBalanceAndAssignsRanks()
    {
        await _harness.CreateUserAsync("shortstack", 500);
        await _harness.CreateUserAsync("chipboss", 3_000);
        await _harness.CreateUserAsync("midstack", 1_500);
        await _harness.CreateUserAsync("walletless", null);

        var service = _harness.GetRequiredService<LeaderboardService>();

        var leaderboard = await service.GetLeaderboardAsync();

        Assert.Collection(leaderboard,
            entry =>
            {
                Assert.Equal(1, entry.Rank);
                Assert.Equal("chipboss", entry.UserName);
                Assert.Equal(3_000, entry.Chips);
            },
            entry =>
            {
                Assert.Equal(2, entry.Rank);
                Assert.Equal("midstack", entry.UserName);
                Assert.Equal(1_500, entry.Chips);
            },
            entry =>
            {
                Assert.Equal(3, entry.Rank);
                Assert.Equal("shortstack", entry.UserName);
                Assert.Equal(500, entry.Chips);
            },
            entry =>
            {
                Assert.Equal(4, entry.Rank);
                Assert.Equal("walletless", entry.UserName);
                Assert.Equal(0, entry.Chips);
            });
    }

    [Fact]
    public async Task GetLeaderboardAsync_ReflectsRankChangesAfterWalletUpdates()
    {
        var underdog = await _harness.CreateUserAsync("underdog", 1_000);
        await _harness.CreateUserAsync("leader", 2_000);

        var walletService = _harness.GetRequiredService<WalletService>();
        var leaderboardService = _harness.GetRequiredService<LeaderboardService>();

        var before = await leaderboardService.GetLeaderboardAsync();
        Assert.Equal("leader", before[0].UserName);
        Assert.Equal("underdog", before[1].UserName);

        var updated = await walletService.AdjustBalanceAsync(underdog.Id, 1_500, "leaderboard-test");
        Assert.True(updated);

        var after = await leaderboardService.GetLeaderboardAsync();
        Assert.Equal("underdog", after[0].UserName);
        Assert.Equal(2_500, after[0].Chips);
        Assert.Equal(1, after[0].Rank);
        Assert.Equal("leader", after[1].UserName);
        Assert.Equal(2, after[1].Rank);
    }

    public void Dispose()
    {
        _harness.Dispose();
    }
}

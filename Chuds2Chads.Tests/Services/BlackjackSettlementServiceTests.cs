using System;
using System.Linq;
using System.Threading.Tasks;
using Chuds2Chads.Data;
using Chuds2Chads.Data.Entities;
using Chuds2Chads.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Chuds2Chads.Tests.Services
{
    public class BlackjackSettlementServiceTests
    {
        private static async Task<(AppDbContext context, SqliteConnection connection, WalletService walletService, BlackjackSettlementService settlementService, Guid userId)> CreateTestSetupAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new AppDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var testUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "blackjackuser",
                Email = "blackjackuser@example.com"
            };

            context.Users.Add(testUser);
            await context.SaveChangesAsync();

            var walletService = new WalletService(context);
            var settlementService = new BlackjackSettlementService(walletService);

            return (context, connection, walletService, settlementService, testUser.Id);
        }

        [Fact]
        public async Task PlaceBetAndPlayerWin_UpdatesWalletBalanceAndLogsTransactions()
        {
            var (context, connection, walletService, settlementService, userId) = await CreateTestSetupAsync();

            try
            {
                await walletService.EnsureWalletAsync(userId);

                var startingBalance = await walletService.GetBalanceAsync(userId);
                const long stake = 100;

                var placed = await settlementService.PlaceBetAsync(userId, stake, "blackjack-bet-test-1");
                Assert.True(placed);

                var payout = await settlementService.ResolveRoundAsync(
                    userId,
                    stake,
                    BlackjackSettlementOutcome.PlayerWin,
                    "blackjack-payout-test-1");

                Assert.Equal(200, payout);

                var endingBalance = await walletService.GetBalanceAsync(userId);
                Assert.Equal(startingBalance - stake + payout, endingBalance);

                var transactions = await context.Transactions
                    .Where(t => t.UserId == userId)
                    .ToListAsync();

                Assert.Contains(transactions, t => t.Reference == "blackjack-bet-test-1");
                Assert.Contains(transactions, t => t.Reference == "blackjack-payout-test-1");
                Assert.Contains(transactions, t => t.Type == TransactionType.BetPlaced);
                Assert.Contains(transactions, t => t.Type == TransactionType.Payout);
            }
            finally
            {
                await context.DisposeAsync();
                await connection.DisposeAsync();
            }
        }

        [Fact]
        public async Task PlaceBetAndDealerWin_OnlyDeductsStake()
        {
            var (context, connection, walletService, settlementService, userId) = await CreateTestSetupAsync();

            try
            {
                await walletService.EnsureWalletAsync(userId);

                var startingBalance = await walletService.GetBalanceAsync(userId);
                const long stake = 100;

                var placed = await settlementService.PlaceBetAsync(userId, stake, "blackjack-bet-test-2");
                Assert.True(placed);

                var payout = await settlementService.ResolveRoundAsync(
                    userId,
                    stake,
                    BlackjackSettlementOutcome.DealerWin,
                    "blackjack-payout-test-2");

                Assert.Equal(0, payout);

                var endingBalance = await walletService.GetBalanceAsync(userId);
                Assert.Equal(startingBalance - stake, endingBalance);

                var transactions = await context.Transactions
                    .Where(t => t.UserId == userId)
                    .ToListAsync();

                Assert.Contains(transactions, t => t.Reference == "blackjack-bet-test-2");
                Assert.DoesNotContain(transactions, t => t.Reference == "blackjack-payout-test-2");
            }
            finally
            {
                await context.DisposeAsync();
                await connection.DisposeAsync();
            }
        }

        [Fact]
        public async Task PlaceBetAndPush_ReturnsStakeOnly()
        {
            var (context, connection, walletService, settlementService, userId) = await CreateTestSetupAsync();

            try
            {
                await walletService.EnsureWalletAsync(userId);

                var startingBalance = await walletService.GetBalanceAsync(userId);
                const long stake = 100;

                var placed = await settlementService.PlaceBetAsync(userId, stake, "blackjack-bet-test-3");
                Assert.True(placed);

                var payout = await settlementService.ResolveRoundAsync(
                    userId,
                    stake,
                    BlackjackSettlementOutcome.Push,
                    "blackjack-payout-test-3");

                Assert.Equal(100, payout);

                var endingBalance = await walletService.GetBalanceAsync(userId);
                Assert.Equal(startingBalance, endingBalance);

                var transactions = await context.Transactions
                    .Where(t => t.UserId == userId)
                    .ToListAsync();

                Assert.Contains(transactions, t => t.Reference == "blackjack-bet-test-3");
                Assert.Contains(transactions, t => t.Reference == "blackjack-payout-test-3");
            }
            finally
            {
                await context.DisposeAsync();
                await connection.DisposeAsync();
            }
        }
    }
}
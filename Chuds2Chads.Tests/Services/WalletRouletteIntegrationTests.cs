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
    public class WalletRouletteIntegrationTests
    {
        private static async Task<(AppDbContext context, SqliteConnection connection, WalletService walletService, RouletteService rouletteService, Guid userId)> CreateTestSetupAsync()
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
                UserName = "testuser",
                Email = "test@example.com"
            };

            context.Users.Add(testUser);
            await context.SaveChangesAsync();

            var walletService = new WalletService(context);
            var rouletteService = new RouletteService();

            return (context, connection, walletService, rouletteService, testUser.Id);
        }

        [Fact]
        public async Task Roulette_Win_UpdatesWalletBalanceAndTransactionsCorrectly()
        {
            var (context, connection, walletService, rouletteService, userId) = await CreateTestSetupAsync();

            try
            {
                await walletService.EnsureWalletAsync(userId);

                var startingBalance = await walletService.GetBalanceAsync(userId);

                var betPlaced = await walletService.TryPlaceBetAsync(userId, 100, "roulette-bet-test-1");
                Assert.True(betPlaced);

                var outcome = rouletteService.ResolveBet(BetType.Red, null, 1);
                Assert.True(outcome.Won);

                var payout = 100 * (outcome.PayoutMultiplier + 1);

                await walletService.CreditPayoutAsync(userId, payout, "roulette-payout-test-1");

                var endingBalance = await walletService.GetBalanceAsync(userId);
                Assert.Equal(startingBalance - 100 + payout, endingBalance);

                var transactions = await context.Transactions
                    .Where(t => t.UserId == userId)
                    .ToListAsync();

                Assert.Contains(transactions, t => t.Reference == "roulette-bet-test-1");
                Assert.Contains(transactions, t => t.Reference == "roulette-payout-test-1");
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
        public async Task Roulette_Loss_OnlyDeductsBetAndDoesNotCreatePayoutTransaction()
        {
            var (context, connection, walletService, rouletteService, userId) = await CreateTestSetupAsync();

            try
            {
                await walletService.EnsureWalletAsync(userId);

                var startingBalance = await walletService.GetBalanceAsync(userId);

                var betPlaced = await walletService.TryPlaceBetAsync(userId, 100, "roulette-bet-test-2");
                Assert.True(betPlaced);

                var outcome = rouletteService.ResolveBet(BetType.Red, null, 2);
                Assert.False(outcome.Won);

                var endingBalance = await walletService.GetBalanceAsync(userId);
                Assert.Equal(startingBalance - 100, endingBalance);

                var transactions = await context.Transactions
                    .Where(t => t.UserId == userId)
                    .ToListAsync();

                Assert.Contains(transactions, t => t.Reference == "roulette-bet-test-2");
                Assert.DoesNotContain(transactions, t => t.Reference == "roulette-payout-test-2");
                Assert.DoesNotContain(transactions, t => t.Type == TransactionType.Payout && t.Reference == "roulette-payout-test-2");
            }
            finally
            {
                await context.DisposeAsync();
                await connection.DisposeAsync();
            }
        }
    }
}
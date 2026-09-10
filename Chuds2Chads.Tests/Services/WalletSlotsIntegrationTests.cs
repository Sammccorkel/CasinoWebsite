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
    public class WalletSlotsIntegrationTests
    {
        private static async Task<(AppDbContext context, SqliteConnection connection, WalletService walletService, Guid userId)> CreateTestSetupAsync()
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
                UserName = "slotuser",
                Email = "slotuser@example.com"
            };

            context.Users.Add(testUser);
            await context.SaveChangesAsync();

            var walletService = new WalletService(context);

            return (context, connection, walletService, testUser.Id);
        }

        [Fact]
        public async Task Slots_Jackpot_UpdatesWalletBalanceAndTransactionsCorrectly()
        {
            var (context, connection, walletService, userId) = await CreateTestSetupAsync();

            try
            {
                await walletService.EnsureWalletAsync(userId);

                var startingBalance = await walletService.GetBalanceAsync(userId);

                var betPlaced = await walletService.TryPlaceBetAsync(userId, 100, "slots-bet-test-1");
                Assert.True(betPlaced);

                var outcome = SlotsService.EvaluateSpin(new[] { SlotSymbol.Seven, SlotSymbol.Seven, SlotSymbol.Seven });
                Assert.True(outcome.IsJackpot);
                Assert.Equal(50, outcome.PayoutMultiplier);

                var payout = 100 * outcome.PayoutMultiplier;
                await walletService.CreditPayoutAsync(userId, payout, "slots-payout-test-1");

                var endingBalance = await walletService.GetBalanceAsync(userId);
                Assert.Equal(startingBalance - 100 + payout, endingBalance);

                var transactions = await context.Transactions
                    .Where(t => t.UserId == userId)
                    .ToListAsync();

                Assert.Contains(transactions, t => t.Reference == "slots-bet-test-1");
                Assert.Contains(transactions, t => t.Reference == "slots-payout-test-1");
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
        public async Task Slots_Loss_OnlyDeductsBetAndDoesNotCreatePayoutTransaction()
        {
            var (context, connection, walletService, userId) = await CreateTestSetupAsync();

            try
            {
                await walletService.EnsureWalletAsync(userId);

                var startingBalance = await walletService.GetBalanceAsync(userId);

                var betPlaced = await walletService.TryPlaceBetAsync(userId, 100, "slots-bet-test-2");
                Assert.True(betPlaced);

                var outcome = SlotsService.EvaluateSpin(new[] { SlotSymbol.Cherry, SlotSymbol.Lemon, SlotSymbol.Bell });
                Assert.Equal(0, outcome.PayoutMultiplier);
                Assert.False(outcome.IsJackpot);

                var endingBalance = await walletService.GetBalanceAsync(userId);
                Assert.Equal(startingBalance - 100, endingBalance);

                var transactions = await context.Transactions
                    .Where(t => t.UserId == userId)
                    .ToListAsync();

                Assert.Contains(transactions, t => t.Reference == "slots-bet-test-2");
                Assert.DoesNotContain(transactions, t => t.Reference == "slots-payout-test-2");
                Assert.DoesNotContain(transactions, t => t.Type == TransactionType.Payout && t.Reference == "slots-payout-test-2");
            }
            finally
            {
                await context.DisposeAsync();
                await connection.DisposeAsync();
            }
        }
    }
}
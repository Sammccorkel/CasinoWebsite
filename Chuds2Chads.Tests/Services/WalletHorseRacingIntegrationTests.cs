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
    public class WalletHorseRaceIntegrationTests
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
                UserName = "horseuser",
                Email = "horseuser@example.com"
            };

            context.Users.Add(testUser);
            await context.SaveChangesAsync();

            var walletService = new WalletService(context);

            return (context, connection, walletService, testUser.Id);
        }

        [Fact]
        public async Task HorseRace_Win_CreditsPayoutAndLogsTransactions()
        {
            var (context, connection, walletService, userId) = await CreateTestSetupAsync();

            try
            {
                await walletService.EnsureWalletAsync(userId);

                var startingBalance = await walletService.GetBalanceAsync(userId);
                const long stake = 100;

                var betPlaced = await walletService.TryPlaceBetAsync(userId, stake, "horse-bet-test-1");
                Assert.True(betPlaced);

                var winningHorse = HorseRaceService.Horses[0];
                var payout = (long)(stake * winningHorse.Odds);

                await walletService.CreditPayoutAsync(userId, payout, "horse-payout-test-1");

                var endingBalance = await walletService.GetBalanceAsync(userId);
                Assert.Equal(startingBalance - stake + payout, endingBalance);

                var transactions = await context.Transactions
                    .Where(t => t.UserId == userId)
                    .ToListAsync();

                Assert.Contains(transactions, t => t.Reference == "horse-bet-test-1");
                Assert.Contains(transactions, t => t.Reference == "horse-payout-test-1");
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
        public async Task HorseRace_Loss_OnlyDeductsBetAndDoesNotCreatePayoutTransaction()
        {
            var (context, connection, walletService, userId) = await CreateTestSetupAsync();

            try
            {
                await walletService.EnsureWalletAsync(userId);

                var startingBalance = await walletService.GetBalanceAsync(userId);
                const long stake = 100;

                var betPlaced = await walletService.TryPlaceBetAsync(userId, stake, "horse-bet-test-2");
                Assert.True(betPlaced);

                var endingBalance = await walletService.GetBalanceAsync(userId);
                Assert.Equal(startingBalance - stake, endingBalance);

                var transactions = await context.Transactions
                    .Where(t => t.UserId == userId)
                    .ToListAsync();

                Assert.Contains(transactions, t => t.Reference == "horse-bet-test-2");
                Assert.DoesNotContain(transactions, t => t.Reference == "horse-payout-test-2");
                Assert.DoesNotContain(transactions, t => t.Type == TransactionType.Payout && t.Reference == "horse-payout-test-2");
            }
            finally
            {
                await context.DisposeAsync();
                await connection.DisposeAsync();
            }
        }
    }
}
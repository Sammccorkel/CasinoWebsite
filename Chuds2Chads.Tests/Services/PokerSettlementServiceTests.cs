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
    public class PokerSettlementServiceTests
    {
        private static async Task<(AppDbContext context, SqliteConnection connection, WalletService walletService, PokerSettlementService settlementService, Guid userId)> CreateTestSetupAsync()
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
                UserName = "pokeruser",
                Email = "pokeruser@example.com"
            };

            context.Users.Add(testUser);
            await context.SaveChangesAsync();

            var walletService = new WalletService(context);
            var settlementService = new PokerSettlementService(walletService);

            return (context, connection, walletService, settlementService, testUser.Id);
        }

        [Fact]
        public async Task BuyIn_And_ProfitableCashOut_UpdatesWalletAndLogsTransactions()
        {
            var (context, connection, walletService, settlementService, userId) = await CreateTestSetupAsync();

            try
            {
                await walletService.EnsureWalletAsync(userId);

                var startingBalance = await walletService.GetBalanceAsync(userId);
                const long buyIn = 200;
                const long endingStack = 350;

                var boughtIn = await settlementService.BuyInAsync(userId, buyIn, "poker-buyin-test-1");
                Assert.True(boughtIn);

                var cashedOut = await settlementService.CashOutAsync(userId, endingStack, "poker-cashout-test-1");
                Assert.Equal(endingStack, cashedOut);

                var endingBalance = await walletService.GetBalanceAsync(userId);
                Assert.Equal(startingBalance - buyIn + endingStack, endingBalance);
                Assert.Equal(150, settlementService.CalculateNetResult(buyIn, endingStack));

                var transactions = await context.Transactions
                    .Where(t => t.UserId == userId)
                    .ToListAsync();

                Assert.Contains(transactions, t => t.Reference == "poker-buyin-test-1");
                Assert.Contains(transactions, t => t.Reference == "poker-cashout-test-1");
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
        public async Task BuyIn_And_LosingCashOut_StillReturnsRemainingStack()
        {
            var (context, connection, walletService, settlementService, userId) = await CreateTestSetupAsync();

            try
            {
                await walletService.EnsureWalletAsync(userId);

                var startingBalance = await walletService.GetBalanceAsync(userId);
                const long buyIn = 200;
                const long endingStack = 75;

                var boughtIn = await settlementService.BuyInAsync(userId, buyIn, "poker-buyin-test-2");
                Assert.True(boughtIn);

                var cashedOut = await settlementService.CashOutAsync(userId, endingStack, "poker-cashout-test-2");
                Assert.Equal(endingStack, cashedOut);

                var endingBalance = await walletService.GetBalanceAsync(userId);
                Assert.Equal(startingBalance - buyIn + endingStack, endingBalance);
                Assert.Equal(-125, settlementService.CalculateNetResult(buyIn, endingStack));

                var transactions = await context.Transactions
                    .Where(t => t.UserId == userId)
                    .ToListAsync();

                Assert.Contains(transactions, t => t.Reference == "poker-buyin-test-2");
                Assert.Contains(transactions, t => t.Reference == "poker-cashout-test-2");
            }
            finally
            {
                await context.DisposeAsync();
                await connection.DisposeAsync();
            }
        }

        [Fact]
        public async Task BuyIn_And_BustCashOut_OnlyDeductsBuyIn()
        {
            var (context, connection, walletService, settlementService, userId) = await CreateTestSetupAsync();

            try
            {
                await walletService.EnsureWalletAsync(userId);

                var startingBalance = await walletService.GetBalanceAsync(userId);
                const long buyIn = 200;
                const long endingStack = 0;

                var boughtIn = await settlementService.BuyInAsync(userId, buyIn, "poker-buyin-test-3");
                Assert.True(boughtIn);

                var cashedOut = await settlementService.CashOutAsync(userId, endingStack, "poker-cashout-test-3");
                Assert.Equal(0, cashedOut);

                var endingBalance = await walletService.GetBalanceAsync(userId);
                Assert.Equal(startingBalance - buyIn, endingBalance);
                Assert.Equal(-200, settlementService.CalculateNetResult(buyIn, endingStack));

                var transactions = await context.Transactions
                    .Where(t => t.UserId == userId)
                    .ToListAsync();

                Assert.Contains(transactions, t => t.Reference == "poker-buyin-test-3");
                Assert.DoesNotContain(transactions, t => t.Reference == "poker-cashout-test-3");
            }
            finally
            {
                await context.DisposeAsync();
                await connection.DisposeAsync();
            }
        }

        [Fact]
        public async Task BuyIn_And_BreakEvenCashOut_ReturnsOriginalBuyIn()
        {
            var (context, connection, walletService, settlementService, userId) = await CreateTestSetupAsync();

            try
            {
                await walletService.EnsureWalletAsync(userId);

                var startingBalance = await walletService.GetBalanceAsync(userId);
                const long buyIn = 200;
                const long endingStack = 200;

                var boughtIn = await settlementService.BuyInAsync(userId, buyIn, "poker-buyin-test-4");
                Assert.True(boughtIn);

                var cashedOut = await settlementService.CashOutAsync(userId, endingStack, "poker-cashout-test-4");
                Assert.Equal(endingStack, cashedOut);

                var endingBalance = await walletService.GetBalanceAsync(userId);
                Assert.Equal(startingBalance, endingBalance);
                Assert.Equal(0, settlementService.CalculateNetResult(buyIn, endingStack));

                var transactions = await context.Transactions
                    .Where(t => t.UserId == userId)
                    .ToListAsync();

                Assert.Contains(transactions, t => t.Reference == "poker-buyin-test-4");
                Assert.Contains(transactions, t => t.Reference == "poker-cashout-test-4");
            }
            finally
            {
                await context.DisposeAsync();
                await connection.DisposeAsync();
            }
        }
    }
}
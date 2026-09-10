using System;
using System.Linq;
using System.Threading.Tasks;
using Chuds2Chads.Data;
using Chuds2Chads.Data.Entities;
using Chuds2Chads.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Chuds2Chads.Tests.Services;

public class WalletServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly WalletService _walletService;
    private readonly Guid _userId;

    public WalletServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        // ✅ Create a valid user for foreign key constraints
        var testUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            Email = "test@test.com"
        };

        _db.Users.Add(testUser);
        _db.SaveChanges();

        _userId = testUser.Id;

        _walletService = new WalletService(_db);
    }

    [Fact]
    public async Task EnsureWalletAsync_WhenWalletDoesNotExist_CreatesWalletWithStartingBalance()
    {
        await _walletService.EnsureWalletAsync(_userId, 1000);

        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == _userId);
        Assert.NotNull(wallet);
        Assert.Equal(1000, wallet!.Balance);

        var transaction = await _db.Transactions.FirstOrDefaultAsync(t => t.UserId == _userId);
        Assert.NotNull(transaction);
        Assert.Equal(TransactionType.Deposit, transaction!.Type);
        Assert.Equal(1000, transaction.Amount);
        Assert.Equal(1000, transaction.BalanceAfter);
        Assert.Equal("welcome-bonus", transaction.Reference);
    }

    [Fact]
    public async Task EnsureWalletAsync_WhenWalletAlreadyExists_DoesNotCreateDuplicateWallet()
    {
        await _walletService.EnsureWalletAsync(_userId, 1000);
        await _walletService.EnsureWalletAsync(_userId, 5000);

        var wallets = await _db.Wallets.Where(w => w.UserId == _userId).ToListAsync();
        Assert.Single(wallets);
        Assert.Equal(1000, wallets[0].Balance);

        var transactions = await _db.Transactions.Where(t => t.UserId == _userId).ToListAsync();
        Assert.Single(transactions);
    }

    [Fact]
    public async Task TryPlaceBetAsync_WhenBalanceIsEnough_DeductsBalanceAndLogsTransaction()
    {
        await _walletService.EnsureWalletAsync(_userId, 1000);

        var result = await _walletService.TryPlaceBetAsync(_userId, 200, "roulette-spin-1");

        Assert.True(result);

        var wallet = await _db.Wallets.FirstAsync(w => w.UserId == _userId);
        Assert.Equal(800, wallet.Balance);

        var transaction = await _db.Transactions
            .Where(t => t.UserId == _userId && t.Type == TransactionType.BetPlaced)
            .OrderByDescending(t => t.CreatedUtc)
            .FirstOrDefaultAsync();

        Assert.NotNull(transaction);
        Assert.Equal(-200, transaction!.Amount);
        Assert.Equal(800, transaction.BalanceAfter);
        Assert.Equal("roulette-spin-1", transaction.Reference);
    }

    [Fact]
    public async Task TryPlaceBetAsync_WhenBalanceIsTooLow_ReturnsFalseAndDoesNotChangeWallet()
    {
        await _walletService.EnsureWalletAsync(_userId, 100);

        var result = await _walletService.TryPlaceBetAsync(_userId, 200, "roulette-spin-2");

        Assert.False(result);

        var wallet = await _db.Wallets.FirstAsync(w => w.UserId == _userId);
        Assert.Equal(100, wallet.Balance);

        var betTransactions = await _db.Transactions
            .Where(t => t.UserId == _userId && t.Type == TransactionType.BetPlaced)
            .ToListAsync();

        Assert.Empty(betTransactions);
    }

    [Fact]
    public async Task TryPlaceBetAsync_WhenAmountIsZeroOrLess_ReturnsFalse()
    {
        await _walletService.EnsureWalletAsync(_userId, 1000);

        var zeroResult = await _walletService.TryPlaceBetAsync(_userId, 0, "bad-bet-zero");
        var negativeResult = await _walletService.TryPlaceBetAsync(_userId, -50, "bad-bet-negative");

        Assert.False(zeroResult);
        Assert.False(negativeResult);

        var wallet = await _db.Wallets.FirstAsync(w => w.UserId == _userId);
        Assert.Equal(1000, wallet.Balance);
    }

    [Fact]
    public async Task CreditPayoutAsync_WhenWalletExists_AddsBalanceAndLogsTransaction()
    {
        await _walletService.EnsureWalletAsync(_userId, 1000);

        await _walletService.CreditPayoutAsync(_userId, 300, "roulette-win-1");

        var wallet = await _db.Wallets.FirstAsync(w => w.UserId == _userId);
        Assert.Equal(1300, wallet.Balance);

        var transaction = await _db.Transactions
            .Where(t => t.UserId == _userId && t.Type == TransactionType.Payout)
            .OrderByDescending(t => t.CreatedUtc)
            .FirstOrDefaultAsync();

        Assert.NotNull(transaction);
        Assert.Equal(300, transaction!.Amount);
        Assert.Equal(1300, transaction.BalanceAfter);
        Assert.Equal("roulette-win-1", transaction.Reference);
    }

    [Fact]
    public async Task CreditPayoutAsync_WhenWalletDoesNotExist_CreatesWalletAndAddsBalance()
    {
        await _walletService.CreditPayoutAsync(_userId, 400, "slots-win-1");

        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == _userId);
        Assert.NotNull(wallet);
        Assert.Equal(400, wallet!.Balance);

        var transaction = await _db.Transactions
            .Where(t => t.UserId == _userId && t.Type == TransactionType.Payout)
            .FirstOrDefaultAsync();

        Assert.NotNull(transaction);
        Assert.Equal(400, transaction!.Amount);
        Assert.Equal(400, transaction.BalanceAfter);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}

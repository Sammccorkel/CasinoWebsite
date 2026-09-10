using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Chuds2Chads.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Chuds2Chads.Data;

namespace Chuds2Chads.Services
{
    public class PlayerStatisticsDto
    {
        public string UserName { get; set; } = string.Empty;

        public long TotalWagered { get; set; }
        public long TotalWon { get; set; }
        public long TotalLost { get; set; }
        public long NetResult { get; set; }

        public int TotalGamesPlayed { get; set; }
        public int TotalWins { get; set; }
        public int TotalLosses { get; set; }
        public int CurrentWinStreak { get; set; }
        public int BestWinStreak { get; set; }

        public double OverallWinLossRatio =>
            TotalLosses == 0 ? TotalWins : (double)TotalWins / TotalLosses;

        public string MostPlayedGame { get; set; } = "N/A";

        public List<GameStatisticsDto> GameStats { get; set; } = new();
    }

    public class GameStatisticsDto
    {
        public string GameName { get; set; } = string.Empty;
        public int TimesPlayed { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }

        public long AmountWagered { get; set; }
        public long AmountWon { get; set; }
        public long AmountLost { get; set; }

        public double WinLossRatio =>
            Losses == 0 ? Wins : (double)Wins / Losses;
    }

    public interface IPlayerStatisticsService
    {
        Task<PlayerStatisticsDto> GetPlayerStatisticsAsync(Guid userId);
    }

    public class PlayerStatisticsService : IPlayerStatisticsService
    {
        private readonly AppDbContext _context;

        public PlayerStatisticsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PlayerStatisticsDto> GetPlayerStatisticsAsync(Guid userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);

            var transactions = await _context.Transactions
                .Where(t => t.UserId == userId)
                .ToListAsync();

            var betTransactions = transactions
                .Where(t => t.Type.ToString() == "BetPlaced")
                .ToList();

            var payoutTransactions = transactions
                .Where(t => t.Type.ToString() == "Payout")
                .ToList();

            long totalWagered = betTransactions.Sum(t => Math.Abs(t.Amount));
            long totalWon = payoutTransactions.Sum(t => t.Amount);
            long totalLost = Math.Max(0, totalWagered - totalWon);
            long netResult = totalWon - totalWagered;

            var gameGroups = transactions
                .Where(t => !string.IsNullOrWhiteSpace(t.Reference))
                .GroupBy(t => GetGameNameFromReference(t.Reference!))
                .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                .ToList();

            var gameStats = new List<GameStatisticsDto>();

            foreach (var group in gameGroups)
            {
                var gameBets = group.Where(t => t.Type.ToString() == "BetPlaced").ToList();
                var gamePayouts = group.Where(t => t.Type.ToString() == "Payout").ToList();

                var timesPlayed = gameBets.Count;
                int wins = Math.Min(gameBets.Count, gamePayouts.Count);
                int losses = Math.Max(0, timesPlayed - wins);

                long amountWagered = gameBets.Sum(t => t.Amount);
long amountWon = gamePayouts.Sum(t => t.Amount);
long amountLost = Math.Max(0, amountWagered - amountWon);

                gameStats.Add(new GameStatisticsDto
                {
                    GameName = group.Key,
                    TimesPlayed = timesPlayed,
                    Wins = wins,
                    Losses = losses,
                    AmountWagered = amountWagered,
                    AmountWon = amountWon,
                    AmountLost = amountLost
                });
            }

            var mostPlayedGame = gameStats
                .OrderByDescending(g => g.TimesPlayed)
                .Select(g => g.GameName)
                .FirstOrDefault() ?? "N/A";

            int totalGamesPlayed = gameStats.Sum(g => g.TimesPlayed);
            int totalWins = gameStats.Sum(g => g.Wins);
            int totalLosses = gameStats.Sum(g => g.Losses);
            var streaks = CalculateWinStreaks(transactions);

            return new PlayerStatisticsDto
            {
                UserName = user?.UserName ?? "Unknown",
                TotalWagered = totalWagered,
                TotalWon = totalWon,
                TotalLost = totalLost,
                NetResult = netResult,
                TotalGamesPlayed = totalGamesPlayed,
                TotalWins = totalWins,
                TotalLosses = totalLosses,
                CurrentWinStreak = streaks.Current,
                BestWinStreak = streaks.Best,
                MostPlayedGame = mostPlayedGame,
                GameStats = gameStats.OrderByDescending(g => g.TimesPlayed).ToList()
            };
        }

        private static (int Current, int Best) CalculateWinStreaks(List<Transaction> transactions)
        {
            if (transactions.Count == 0)
            {
                return (0, 0);
            }

            var orderedTransactions = transactions
                .Where(t => !string.IsNullOrWhiteSpace(t.Reference))
                .OrderBy(t => t.CreatedUtc)
                .ToList();

            var bets = orderedTransactions
                .Where(t => t.Type == TransactionType.BetPlaced)
                .ToList();

            if (bets.Count == 0)
            {
                return (0, 0);
            }

            var payouts = orderedTransactions
                .Where(t => t.Type == TransactionType.Payout)
                .ToList();

            var results = new List<bool>(bets.Count);

            for (var index = 0; index < bets.Count; index++)
            {
                var currentBet = bets[index];
                var nextBetUtc = index + 1 < bets.Count ? bets[index + 1].CreatedUtc : DateTime.MaxValue;
                var betGame = GetGameNameFromReference(currentBet.Reference!);

                var payout = payouts.FirstOrDefault(p =>
                    p.CreatedUtc >= currentBet.CreatedUtc &&
                    p.CreatedUtc < nextBetUtc &&
                    string.Equals(GetGameNameFromReference(p.Reference ?? string.Empty), betGame, StringComparison.OrdinalIgnoreCase));

                results.Add(payout is not null && payout.Amount > Math.Abs(currentBet.Amount));
            }

            var best = 0;
            var running = 0;
            foreach (var result in results)
            {
                if (result)
                {
                    running++;
                    best = Math.Max(best, running);
                }
                else
                {
                    running = 0;
                }
            }

            var current = 0;
            for (var index = results.Count - 1; index >= 0; index--)
            {
                if (!results[index])
                {
                    break;
                }

                current++;
            }

            return (current, best);
        }

        private static string GetGameNameFromReference(string reference)
        {
            reference = reference.ToLowerInvariant();

            if (reference.Contains("roulette")) return "Roulette";
            if (reference.Contains("slots")) return "Slots";
            if (reference.Contains("horse")) return "Horse Race";
            if (reference.Contains("blackjack")) return "Blackjack";
            if (reference.Contains("poker")) return "Poker";

            return "Other";
        }
    }
}

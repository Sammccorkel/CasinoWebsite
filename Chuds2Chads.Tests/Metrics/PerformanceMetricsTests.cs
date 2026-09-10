using System.Collections.Generic;
using System.Diagnostics;
using Chuds2Chads.Games.Blackjack;
using Chuds2Chads.Games.Poker;
using Chuds2Chads.Services;
using Xunit;

namespace Chuds2Chads.Tests.Metrics
{
    public class PerformanceMetricsTests
    {
        [Fact]
        public void Roulette_Spin_100000_CompletesWithinReasonableTime()
        {
            var service = new RouletteService();

            var sw = Stopwatch.StartNew();

            for (int i = 0; i < 100_000; i++)
            {
                _ = service.Spin();
            }

            sw.Stop();

            Assert.True(sw.ElapsedMilliseconds < 3000,
                $"Roulette spins took too long: {sw.ElapsedMilliseconds} ms");
        }

        [Fact]
        public void Slots_Spin_50000_CompletesWithinReasonableTime()
        {
            var service = new SlotsService();

            var sw = Stopwatch.StartNew();

            for (int i = 0; i < 50_000; i++)
            {
                _ = service.Spin();
            }

            sw.Stop();

            Assert.True(sw.ElapsedMilliseconds < 3000,
                $"Slots spins took too long: {sw.ElapsedMilliseconds} ms");
        }

        [Fact]
        public void PokerHandEvaluation_10000_CompletesWithinReasonableTime()
        {
            var player = new PokerPlayer
            {
                Name = "Alice",
                Hand = new List<Card>
                {
                    new Card { Suit = Suit.Spades, Rank = Rank.Ace },
                    new Card { Suit = Suit.Hearts, Rank = Rank.Ace }
                }
            };

            var communityCards = new List<Card>
            {
                new Card { Suit = Suit.Clubs, Rank = Rank.King },
                new Card { Suit = Suit.Diamonds, Rank = Rank.King },
                new Card { Suit = Suit.Spades, Rank = Rank.Two },
                new Card { Suit = Suit.Hearts, Rank = Rank.Nine },
                new Card { Suit = Suit.Clubs, Rank = Rank.Three }
            };

            var sw = Stopwatch.StartNew();

            for (int i = 0; i < 10_000; i++)
            {
                _ = PokerHandEvaluator.Evaluate(player, communityCards);
            }

            sw.Stop();

            Assert.True(sw.ElapsedMilliseconds < 3000,
                $"Poker hand evaluation took too long: {sw.ElapsedMilliseconds} ms");
        }

        [Fact]
        public void Blackjack_StartNewRound_1000Times_CompletesWithinReasonableTime()
        {
            var game = new PlayBlackjack("Metrics Table");
            game.Players.Add(new Player { Name = "Alice" });
            game.Players.Add(new Player { Name = "Bob" });

            var sw = Stopwatch.StartNew();

            for (int i = 0; i < 1000; i++)
            {
                game.StartNewRound();
            }

            sw.Stop();

            Assert.True(sw.ElapsedMilliseconds < 3000,
                $"Blackjack StartNewRound took too long: {sw.ElapsedMilliseconds} ms");
        }
    }
}
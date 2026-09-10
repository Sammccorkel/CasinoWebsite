using System.Collections.Generic;
using Chuds2Chads.Games.Blackjack;
using Chuds2Chads.Games.Poker;
using Xunit;

namespace Chuds2Chads.Tests.Games
{
    public class PokerHandEvaluatorTests
    {
        [Fact]
        public void Evaluate_HighCard_IsRecognized()
        {
            var player = new PokerPlayer
            {
                Name = "Alice",
                Hand = new List<Card>
                {
                    new Card { Suit = Suit.Spades, Rank = Rank.Ace },
                    new Card { Suit = Suit.Hearts, Rank = Rank.Ten }
                }
            };

            var communityCards = new List<Card>
            {
                new Card { Suit = Suit.Clubs, Rank = Rank.Seven },
                new Card { Suit = Suit.Diamonds, Rank = Rank.Four },
                new Card { Suit = Suit.Spades, Rank = Rank.Two },
                new Card { Suit = Suit.Hearts, Rank = Rank.Nine },
                new Card { Suit = Suit.Clubs, Rank = Rank.Three }
            };

            var result = PokerHandEvaluator.Evaluate(player, communityCards);

            Assert.Equal(HandRank.HighCard, result.Rank);
        }

        [Fact]
        public void Evaluate_Pair_IsRecognized()
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
                new Card { Suit = Suit.Clubs, Rank = Rank.Seven },
                new Card { Suit = Suit.Diamonds, Rank = Rank.Four },
                new Card { Suit = Suit.Spades, Rank = Rank.Two },
                new Card { Suit = Suit.Hearts, Rank = Rank.Nine },
                new Card { Suit = Suit.Clubs, Rank = Rank.Three }
            };

            var result = PokerHandEvaluator.Evaluate(player, communityCards);

            Assert.Equal(HandRank.Pair, result.Rank);
        }

        [Fact]
        public void Evaluate_TwoPair_IsRecognized()
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

            var result = PokerHandEvaluator.Evaluate(player, communityCards);

            Assert.Equal(HandRank.TwoPair, result.Rank);
        }

        [Fact]
        public void Evaluate_ThreeOfAKind_IsRecognized()
        {
            var player = new PokerPlayer
            {
                Name = "Alice",
                Hand = new List<Card>
                {
                    new Card { Suit = Suit.Spades, Rank = Rank.Queen },
                    new Card { Suit = Suit.Hearts, Rank = Rank.Queen }
                }
            };

            var communityCards = new List<Card>
            {
                new Card { Suit = Suit.Clubs, Rank = Rank.Queen },
                new Card { Suit = Suit.Diamonds, Rank = Rank.Four },
                new Card { Suit = Suit.Spades, Rank = Rank.Two },
                new Card { Suit = Suit.Hearts, Rank = Rank.Nine },
                new Card { Suit = Suit.Clubs, Rank = Rank.Three }
            };

            var result = PokerHandEvaluator.Evaluate(player, communityCards);

            Assert.Equal(HandRank.ThreeOfAKind, result.Rank);
        }

        [Fact]
        public void Evaluate_Straight_IsRecognized()
        {
            var player = new PokerPlayer
            {
                Name = "Alice",
                Hand = new List<Card>
                {
                    new Card { Suit = Suit.Spades, Rank = Rank.Six },
                    new Card { Suit = Suit.Hearts, Rank = Rank.Five }
                }
            };

            var communityCards = new List<Card>
            {
                new Card { Suit = Suit.Clubs, Rank = Rank.Four },
                new Card { Suit = Suit.Diamonds, Rank = Rank.Three },
                new Card { Suit = Suit.Spades, Rank = Rank.Two },
                new Card { Suit = Suit.Hearts, Rank = Rank.King },
                new Card { Suit = Suit.Clubs, Rank = Rank.Nine }
            };

            var result = PokerHandEvaluator.Evaluate(player, communityCards);

            Assert.Equal(HandRank.Straight, result.Rank);
        }

        [Fact]
        public void Evaluate_Flush_IsRecognized()
        {
            var player = new PokerPlayer
            {
                Name = "Alice",
                Hand = new List<Card>
                {
                    new Card { Suit = Suit.Spades, Rank = Rank.Ace },
                    new Card { Suit = Suit.Spades, Rank = Rank.Ten }
                }
            };

            var communityCards = new List<Card>
            {
                new Card { Suit = Suit.Spades, Rank = Rank.Seven },
                new Card { Suit = Suit.Spades, Rank = Rank.Four },
                new Card { Suit = Suit.Spades, Rank = Rank.Two },
                new Card { Suit = Suit.Hearts, Rank = Rank.King },
                new Card { Suit = Suit.Clubs, Rank = Rank.Nine }
            };

            var result = PokerHandEvaluator.Evaluate(player, communityCards);

            Assert.Equal(HandRank.Flush, result.Rank);
        }

        [Fact]
        public void Evaluate_FullHouse_IsRecognized()
        {
            var player = new PokerPlayer
            {
                Name = "Alice",
                Hand = new List<Card>
                {
                    new Card { Suit = Suit.Spades, Rank = Rank.Jack },
                    new Card { Suit = Suit.Hearts, Rank = Rank.Jack }
                }
            };

            var communityCards = new List<Card>
            {
                new Card { Suit = Suit.Clubs, Rank = Rank.Jack },
                new Card { Suit = Suit.Diamonds, Rank = Rank.Nine },
                new Card { Suit = Suit.Spades, Rank = Rank.Nine },
                new Card { Suit = Suit.Hearts, Rank = Rank.King },
                new Card { Suit = Suit.Clubs, Rank = Rank.Two }
            };

            var result = PokerHandEvaluator.Evaluate(player, communityCards);

            Assert.Equal(HandRank.FullHouse, result.Rank);
        }

        [Fact]
        public void Evaluate_FourOfAKind_IsRecognized()
        {
            var player = new PokerPlayer
            {
                Name = "Alice",
                Hand = new List<Card>
                {
                    new Card { Suit = Suit.Spades, Rank = Rank.King },
                    new Card { Suit = Suit.Hearts, Rank = Rank.King }
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

            var result = PokerHandEvaluator.Evaluate(player, communityCards);

            Assert.Equal(HandRank.FourOfAKind, result.Rank);
        }

        [Fact]
        public void Evaluate_StraightFlush_IsRecognized()
        {
            var player = new PokerPlayer
            {
                Name = "Alice",
                Hand = new List<Card>
                {
                    new Card { Suit = Suit.Hearts, Rank = Rank.Nine },
                    new Card { Suit = Suit.Hearts, Rank = Rank.Eight }
                }
            };

            var communityCards = new List<Card>
            {
                new Card { Suit = Suit.Hearts, Rank = Rank.Seven },
                new Card { Suit = Suit.Hearts, Rank = Rank.Six },
                new Card { Suit = Suit.Hearts, Rank = Rank.Five },
                new Card { Suit = Suit.Spades, Rank = Rank.King },
                new Card { Suit = Suit.Clubs, Rank = Rank.Two }
            };

            var result = PokerHandEvaluator.Evaluate(player, communityCards);

            Assert.Equal(HandRank.StraightFlush, result.Rank);
        }

        [Fact]
        public void Evaluate_RoyalFlush_IsRecognized()
        {
            var player = new PokerPlayer
            {
                Name = "Alice",
                Hand = new List<Card>
                {
                    new Card { Suit = Suit.Spades, Rank = Rank.Ace },
                    new Card { Suit = Suit.Spades, Rank = Rank.King }
                }
            };

            var communityCards = new List<Card>
            {
                new Card { Suit = Suit.Spades, Rank = Rank.Queen },
                new Card { Suit = Suit.Spades, Rank = Rank.Jack },
                new Card { Suit = Suit.Spades, Rank = Rank.Ten },
                new Card { Suit = Suit.Hearts, Rank = Rank.Two },
                new Card { Suit = Suit.Clubs, Rank = Rank.Three }
            };

            var result = PokerHandEvaluator.Evaluate(player, communityCards);

            Assert.Equal(HandRank.RoyalFlush, result.Rank);
        }
    }
}
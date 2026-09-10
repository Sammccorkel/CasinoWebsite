using System.Collections.Generic;
using System.Linq;
using Chuds2Chads.Games.Blackjack;
using Xunit;

namespace Chuds2Chads.Tests
{
    public class BlackjackGameTests
    {
        [Fact]
        public void StartNewRound_WithPlayers_DealsTwoCardsToEachPlayerAndDealer()
        {
            // Use a fixed deck so Alice and Bob are never dealt 21, preventing auto-advance
            var deck = new FakeDeck(new[]
            {
                new Card { Rank = Rank.Five,  Suit = Suit.Hearts   }, // Alice card 1
                new Card { Rank = Rank.Six,   Suit = Suit.Spades   }, // Alice card 2 (11)
                new Card { Rank = Rank.Seven, Suit = Suit.Hearts   }, // Bob card 1
                new Card { Rank = Rank.Eight, Suit = Suit.Spades   }, // Bob card 2 (15)
                new Card { Rank = Rank.Nine,  Suit = Suit.Hearts   }, // Dealer card 1
                new Card { Rank = Rank.Ten,   Suit = Suit.Spades   }, // Dealer card 2 (19)
            });
            var game = new PlayBlackjack("Test Lobby", deck);
            game.Players.Add(new Player { Name = "Alice" });
            game.Players.Add(new Player { Name = "Bob" });

            game.StartNewRound();

            Assert.Equal(GamePhase.PlayerTurn, game.Phase);
            Assert.Equal(2, game.Players[0].Hand.Count);
            Assert.Equal(2, game.Players[1].Hand.Count);
            Assert.Equal(2, game.Dealer.Hand.Count);
            Assert.False(game.DealerRevealed);
            Assert.Equal("Alice", game.CurrentPlayer?.Name);
        }

        [Fact]
        public void StartNewRound_WithNoPlayers_DoesNothing()
        {
            var game = new PlayBlackjack("Empty Lobby");

            game.StartNewRound();

            Assert.Equal(GamePhase.WaitingForPlayers, game.Phase);
            Assert.Empty(game.Dealer.Hand);
            Assert.Null(game.CurrentPlayer);
        }

        [Fact]
        public void PlayerHit_OnCurrentPlayersTurn_AddsOneCard()
        {
            var game = new PlayBlackjack("Test Lobby");
            game.Players.Add(new Player { Name = "Alice" });

            game.StartNewRound();
            int before = game.CurrentPlayer!.Hand.Count;

            game.PlayerHit("Alice");

            Assert.Equal(before + 1, game.Players[0].Hand.Count);
        }

        [Fact]
public void PlayerHit_WrongPlayerName_DoesNothing()
{
    var game = new PlayBlackjack("Test Lobby");
    game.Players.Add(new Player { Name = "Alice" });

    game.StartNewRound();

    Assert.NotNull(game.CurrentPlayer);
    int before = game.CurrentPlayer!.Hand.Count;

    game.PlayerHit("Bob");

    Assert.Equal(before, game.Players[0].Hand.Count);
    Assert.Equal("Alice", game.CurrentPlayer?.Name);
}

        [Fact]
        public void PlayerStand_CurrentPlayer_MovesToNextPlayer()
        {
            var game = new PlayBlackjack("Test Lobby");
            game.Players.Add(new Player { Name = "Alice" });
            game.Players.Add(new Player { Name = "Bob" });

            game.StartNewRound();

            game.PlayerStand("Alice");

            Assert.True(game.Players[0].HasStood);
            Assert.Equal("Bob", game.CurrentPlayer?.Name);
            Assert.Equal(GamePhase.PlayerTurn, game.Phase);
        }

        [Fact]
        public void PlayerStand_LastPlayer_TriggersDealerTurnAndEndsGame()
        {
            var game = new PlayBlackjack("Test Lobby");
            game.Players.Add(new Player { Name = "Alice" });

            game.StartNewRound();

            game.PlayerStand("Alice");

            Assert.Equal(GamePhase.GameOver, game.Phase);
            Assert.True(game.DealerRevealed);
            Assert.True(game.Dealer.Score >= 17 || game.Dealer.IsBusted);
        }

        [Fact]
        public void Player_Reset_ClearsRoundState()
        {
            var player = new Player
            {
                Name = "Alice",
                CurrentBet = 100,
                IsReady = true,
                HasStood = true,
                HasWon = true,
                IsPush = true
            };

            player.Hit(new Card { Rank = Rank.King, Suit = Suit.Spades });
            player.Hit(new Card { Rank = Rank.Ace, Suit = Suit.Hearts });

            player.Reset();

            Assert.Empty(player.Hand);
            Assert.Equal(0, player.CurrentBet);
            Assert.False(player.IsReady);
            Assert.False(player.HasStood);
            Assert.False(player.HasWon);
            Assert.False(player.IsPush);
        }

        [Fact]
        public void GetHandValue_AceAdjustsFromElevenToOne_WhenNeeded()
        {
            var player = new Player { Name = "Alice" };
            player.Hit(new Card { Rank = Rank.Ace, Suit = Suit.Spades });
            player.Hit(new Card { Rank = Rank.Nine, Suit = Suit.Hearts });
            player.Hit(new Card { Rank = Rank.Five, Suit = Suit.Clubs });

            Assert.Equal(15, player.Score);
            Assert.False(player.IsBusted);
        }

        [Fact]
        public void GetHandValue_MultipleAces_AdjustCorrectly()
        {
            var player = new Player { Name = "Alice" };
            player.Hit(new Card { Rank = Rank.Ace, Suit = Suit.Spades });
            player.Hit(new Card { Rank = Rank.Ace, Suit = Suit.Hearts });
            player.Hit(new Card { Rank = Rank.Nine, Suit = Suit.Clubs });

            Assert.Equal(21, player.Score);
            Assert.False(player.IsBusted);
        }

        [Fact]
        public void Deck_DrawCard_ReturnsCard()
        {
            var deck = new Deck();

            var card = deck.DrawCard();

            Assert.NotNull(card);
            Assert.True(card.Rank >= Rank.Two && card.Rank <= Rank.Ace);
        }

        [Fact]
        public void Card_Value_ReturnsTenForFaceCardsAndElevenForAce()
        {
            var king = new Card { Rank = Rank.King, Suit = Suit.Spades };
            var queen = new Card { Rank = Rank.Queen, Suit = Suit.Hearts };
            var jack = new Card { Rank = Rank.Jack, Suit = Suit.Clubs };
            var ace = new Card { Rank = Rank.Ace, Suit = Suit.Diamonds };

            Assert.Equal(10, king.Value);
            Assert.Equal(10, queen.Value);
            Assert.Equal(10, jack.Value);
            Assert.Equal(11, ace.Value);
        }
    }
}

internal class FakeDeck : IDeck
{
    private readonly Queue<Card> _cards;

    public FakeDeck(IEnumerable<Card> cards) => _cards = new Queue<Card>(cards);

    public void InitializeAndShuffle() { } // no-op: keeps the preset order intact

    public Card DrawCard() => _cards.Dequeue();
}
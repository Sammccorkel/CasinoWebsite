using System;
using System.Collections.Generic;
using System.Linq;

namespace Chuds2Chads.Games.Blackjack
{
    public enum Suit { Hearts, Diamonds, Clubs, Spades }
    public enum Rank { Two = 2, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King, Ace }
    public enum GamePhase { 
        WaitingForPlayers, 
        Dealing, 
        PlayerTurn, 
        DealerTurn, 
        GameOver, 
        Finished }

    public class Card
    {
        public Suit Suit { get; set; }
        public Rank Rank { get; set; }

        public int Value => Rank == Rank.Ace ? 11 : ((int)Rank <= 10 ? (int)Rank : 10);

        public string ImagePath
        {
            get
            {
                string suitString = Suit.ToString().ToLower();
                string rankString = Rank switch
                {
                    Rank.Ace => "A", Rank.King => "K", Rank.Queen => "Q", Rank.Jack => "J",
                    _ => ((int)Rank).ToString()
                };
                return $"/PlayingCards/{suitString}_{rankString}.png"; 
            }
        }
        public static string CardBackPath => "/PlayingCards/back_light.png";
    }

    public interface IDeck
    {
        void InitializeAndShuffle();
        Card DrawCard();
    }

    public class Deck : IDeck
    {
        private List<Card> _cards = new();
        private Random _random = new();

        public Deck() => InitializeAndShuffle();

        public void InitializeAndShuffle()
        {
            _cards.Clear();
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                foreach (Rank rank in Enum.GetValues(typeof(Rank)))
                {
                    _cards.Add(new Card { Suit = suit, Rank = rank });
                }
            }
            _cards = _cards.OrderBy(x => _random.Next()).ToList();
        }

        public Card DrawCard()
        {
            if (_cards.Count == 0) InitializeAndShuffle();
            var card = _cards[0];
            _cards.RemoveAt(0);
            return card;
        }
    }

    public class Player
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<Card> Hand { get; set; } = new();
        public bool IsDealer { get; set; }
        public long CurrentBet { get; set; }
        public bool IsDisconnected { get; set; }
        public bool IsReady { get; set; }
        public bool HasStood { get; set; }
        public bool HasWon { get; set; }
        public bool IsPush { get; set; }
        
        public int Score => GetHandValue(); 
        public bool IsBusted => Score > 21;

        public int GetHandValue()
        {
            int total = Hand.Sum(c => c.Value);
            int aceCount = Hand.Count(c => c.Rank == Rank.Ace);

            while (total > 21 && aceCount > 0)
            {
                total -= 10;
                aceCount--;
            }
            return total;
        }

        public void Hit(Card card) => Hand.Add(card);

        public void Reset()
        {
            Hand.Clear();
            CurrentBet = 0;
            IsReady = false;
            HasStood = false;
            HasWon = false;
            IsPush = false;
        }
    }

    public class BlackjackTableInfo
    {
        public string LobbyName { get; set; } = string.Empty;
        public List<string> Players { get; set; } = new();
        public int MaxPlayers { get; set; } = 4;
        public bool IsFull => Players.Count >= MaxPlayers;
    }
}

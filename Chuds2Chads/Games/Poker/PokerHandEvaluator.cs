using System;
using System.Collections.Generic;
using System.Linq;
using Chuds2Chads.Games.Blackjack;

namespace Chuds2Chads.Games.Poker
{
    public enum HandRank
    {
        HighCard, Pair, TwoPair, ThreeOfAKind, Straight, Flush, FullHouse, FourOfAKind, StraightFlush, RoyalFlush
    }

    public class HandEvaluation : IComparable<HandEvaluation>
    {
        public PokerPlayer Player { get; set; } = default!;
        public HandRank Rank { get; set; }
        public List<int> TieBreakers { get; set; } = new();

        // This allows C# to automatically sort hands from worst to best!
        public int CompareTo(HandEvaluation? other)
        {
            if (other == null) return 1;
            
            // First, compare the actual hand type (e.g., Flush beats a Pair)
            if (this.Rank != other.Rank)
                return this.Rank.CompareTo(other.Rank);

            // If it's the same hand type, check the kickers one by one
            for (int i = 0; i < Math.Min(this.TieBreakers.Count, other.TieBreakers.Count); i++)
            {
                if (this.TieBreakers[i] != other.TieBreakers[i])
                    return this.TieBreakers[i].CompareTo(other.TieBreakers[i]);
            }
            return 0; // True Tie
        }
    }

    public static class PokerHandEvaluator
    {
        public static HandEvaluation Evaluate(PokerPlayer player, List<Card> communityCards)
        {
            var allCards = player.Hand.Concat(communityCards).ToList();
            var evaluation = new HandEvaluation { Player = player };

            // Group cards by rank and count them (e.g., find all the 8s)
            var rankGroups = allCards.GroupBy(c => c.Rank)
                                     .OrderByDescending(g => g.Count())
                                     .ThenByDescending(g => (int)g.Key)
                                     .ToList();

            // Group cards by suit to check for flushes
            var flushGroup = allCards.GroupBy(c => c.Suit).FirstOrDefault(g => g.Count() >= 5);
            var isFlush = flushGroup != null;

            // Check for Straights
            var distinctRanks = allCards.Select(c => (int)c.Rank).Distinct().OrderByDescending(r => r).ToList();
            // Handle Ace acting as a '1' for A-2-3-4-5 straights
            if (distinctRanks.Contains((int)Rank.Ace)) distinctRanks.Add(1); 
            
            var straightHighCard = GetStraightHighCard(distinctRanks);
            var isStraight = straightHighCard > 0;

            // 1. Straight Flush / Royal Flush
            if (isFlush)
            {
                var flushRanks = flushGroup!.Select(c => (int)c.Rank).Distinct().OrderByDescending(r => r).ToList();
                if (flushRanks.Contains((int)Rank.Ace)) flushRanks.Add(1);
                var sfHighCard = GetStraightHighCard(flushRanks);
                
                if (sfHighCard > 0)
                {
                    evaluation.Rank = sfHighCard == (int)Rank.Ace ? HandRank.RoyalFlush : HandRank.StraightFlush;
                    evaluation.TieBreakers.Add(sfHighCard);
                    return evaluation;
                }
            }

            // 2. Four of a Kind
            if (rankGroups[0].Count() == 4)
            {
                evaluation.Rank = HandRank.FourOfAKind;
                evaluation.TieBreakers.Add((int)rankGroups[0].Key);
                evaluation.TieBreakers.Add((int)rankGroups[1].Key); // Kicker
                return evaluation;
            }

            // 3. Full House
            if (rankGroups[0].Count() == 3 && rankGroups.Count > 1 && rankGroups[1].Count() >= 2)
            {
                evaluation.Rank = HandRank.FullHouse;
                evaluation.TieBreakers.Add((int)rankGroups[0].Key); // Set of 3
                evaluation.TieBreakers.Add((int)rankGroups[1].Key); // Pair
                return evaluation;
            }

            // 4. Flush
            if (isFlush)
            {
                evaluation.Rank = HandRank.Flush;
                evaluation.TieBreakers = flushGroup!.Select(c => (int)c.Rank).OrderByDescending(r => r).Take(5).ToList();
                return evaluation;
            }

            // 5. Straight
            if (isStraight)
            {
                evaluation.Rank = HandRank.Straight;
                evaluation.TieBreakers.Add(straightHighCard);
                return evaluation;
            }

            // 6. Three of a Kind
            if (rankGroups[0].Count() == 3)
            {
                evaluation.Rank = HandRank.ThreeOfAKind;
                evaluation.TieBreakers.Add((int)rankGroups[0].Key);
                evaluation.TieBreakers.AddRange(rankGroups.Skip(1).Select(g => (int)g.Key).Take(2)); // 2 Kickers
                return evaluation;
            }

            // 7. Two Pair
            if (rankGroups[0].Count() == 2 && rankGroups[1].Count() == 2)
            {
                evaluation.Rank = HandRank.TwoPair;
                evaluation.TieBreakers.Add((int)rankGroups[0].Key); // High Pair
                evaluation.TieBreakers.Add((int)rankGroups[1].Key); // Low Pair
                evaluation.TieBreakers.Add((int)rankGroups[2].Key); // Kicker
                return evaluation;
            }

            // 8. Pair
            if (rankGroups[0].Count() == 2)
            {
                evaluation.Rank = HandRank.Pair;
                evaluation.TieBreakers.Add((int)rankGroups[0].Key);
                evaluation.TieBreakers.AddRange(rankGroups.Skip(1).Select(g => (int)g.Key).Take(3)); // 3 Kickers
                return evaluation;
            }

            // 9. High Card
            evaluation.Rank = HandRank.HighCard;
            evaluation.TieBreakers = rankGroups.Select(g => (int)g.Key).Take(5).ToList();
            return evaluation;
        }

        private static int GetStraightHighCard(List<int> distinctDescendingRanks)
        {
            int consecutiveCount = 1;
            for (int i = 0; i < distinctDescendingRanks.Count - 1; i++)
            {
                if (distinctDescendingRanks[i] == distinctDescendingRanks[i + 1] + 1)
                {
                    consecutiveCount++;
                    if (consecutiveCount == 5) return distinctDescendingRanks[i - 3]; // Return the highest card of the straight
                }
                else
                {
                    consecutiveCount = 1; // Reset
                }
            }
            return 0; // Not a straight
        }
    }
}
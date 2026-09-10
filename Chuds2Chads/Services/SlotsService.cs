namespace Chuds2Chads.Services;

/// <summary>
/// Slot machine game logic lives here, completely separate from the UI.
///
/// 3-reel, 1-payline slot machine with 6 symbol types.
/// Paytable is defined in PayTable - edit there to rebalance without touching the component.
///
/// To add a new symbol: add to SlotSymbol enum, update PayTable, add emoji to SymbolEmoji.
/// To add new reels/paylines: extend SpinReels() and CheckWin().
/// </summary>
public class SlotsService
{
    private readonly Random _rng = new();

    // Reel configuration

    /// <summary>
    /// Each reel's symbol strip. The probability of a symbol landing is
    /// proportional to how many times it appears in the strip.
    /// Lower-value symbols appear more often; jackpot symbols are rare.
    /// To adjust odds, change the counts here.
    /// </summary>
    private static readonly SlotSymbol[][] ReelStrips =
    {
        new[]
        {
            SlotSymbol.Cherry, SlotSymbol.Cherry, SlotSymbol.Cherry,
            SlotSymbol.Lemon, SlotSymbol.Lemon, SlotSymbol.Lemon,
            SlotSymbol.Orange, SlotSymbol.Orange,
            SlotSymbol.Grape, SlotSymbol.Grape,
            SlotSymbol.Bell,
            SlotSymbol.Seven
        },
        new[]
        {
            SlotSymbol.Cherry, SlotSymbol.Cherry,
            SlotSymbol.Lemon, SlotSymbol.Lemon, SlotSymbol.Lemon,
            SlotSymbol.Orange, SlotSymbol.Orange,
            SlotSymbol.Grape, SlotSymbol.Grape,
            SlotSymbol.Bell, SlotSymbol.Bell,
            SlotSymbol.Seven
        },
        new[]
        {
            SlotSymbol.Cherry, SlotSymbol.Cherry,
            SlotSymbol.Lemon, SlotSymbol.Lemon,
            SlotSymbol.Orange, SlotSymbol.Orange,
            SlotSymbol.Grape, SlotSymbol.Grape, SlotSymbol.Grape,
            SlotSymbol.Bell,
            SlotSymbol.Seven, SlotSymbol.Seven
        }
    };

    // Paytable

    /// <summary>
    /// Payout multipliers for three-of-a-kind results.
    /// Multiplier is applied to the original stake, where 2x means win = 2 x stake.
    /// Edit these values to rebalance the game without touching other code.
    /// </summary>
    public static readonly Dictionary<SlotSymbol, int> PayTable = new()
    {
        { SlotSymbol.Cherry, 2 },
        { SlotSymbol.Lemon, 3 },
        { SlotSymbol.Orange, 5 },
        { SlotSymbol.Grape, 8 },
        { SlotSymbol.Bell, 15 },
        { SlotSymbol.Seven, 50 }
    };

    /// <summary>
    /// Payout for two matching cherries, a partial match.
    /// Cherries are special, where two on the first two reels still pays out.
    /// </summary>
    public const int TwoCherryMultiplier = 1;

    /// <summary>
    /// Spins all three reels and returns the result.
    /// The component should call this once per spin button press.
    /// </summary>
    public SpinResult Spin()
    {
        var reels = new SlotSymbol[3];
        for (int i = 0; i < 3; i++)
        {
            var strip = ReelStrips[i];
            reels[i] = strip[_rng.Next(strip.Length)];
        }

        return EvaluateSpin(reels);
    }

    // Internal evaluation

    internal static SpinResult EvaluateSpin(IReadOnlyList<SlotSymbol> reels)
    {
        if (reels.Count != 3)
        {
            throw new ArgumentException("Exactly three reels are required.", nameof(reels));
        }

        bool threeOfAKind = reels[0] == reels[1] && reels[1] == reels[2];

        if (threeOfAKind && PayTable.TryGetValue(reels[0], out int multiplier))
        {
            bool isJackpot = reels[0] == SlotSymbol.Seven;
            return new SpinResult(
                reels.ToArray(),
                multiplier,
                isJackpot,
                isJackpot ? "JACKPOT! Three Sevens!" : $"Three {reels[0]}s! {multiplier}x payout!");
        }

        if (reels[0] == SlotSymbol.Cherry && reels[1] == SlotSymbol.Cherry)
        {
            return new SpinResult(reels.ToArray(), TwoCherryMultiplier, false, "Two Cherries - stake returned!");
        }

        return new SpinResult(reels.ToArray(), 0, false, "No match. Try again!");
    }

    /// <summary>
    /// Maps each symbol to its display emoji for the UI.
    /// </summary>
    public static string GetEmoji(SlotSymbol symbol) => symbol switch
    {
        SlotSymbol.Cherry => "🍒",
        SlotSymbol.Lemon => "🍋",
        SlotSymbol.Orange => "🍊",
        SlotSymbol.Grape => "🍇",
        SlotSymbol.Bell => "🔔",
        SlotSymbol.Seven => "7️⃣",
        _ => "?"
    };
}

/// <summary>
/// The six symbols available on the reels.
/// Ordering matters for display (Cherry = lowest value, Seven = highest).
/// </summary>
public enum SlotSymbol
{
    Cherry,
    Lemon,
    Orange,
    Grape,
    Bell,
    Seven
}

/// <summary>
/// Immutable result of a single spin.
/// The component reads this to update the UI and process the payout.
/// </summary>
public record SpinResult(
    SlotSymbol[] Reels,
    int PayoutMultiplier,
    bool IsJackpot,
    string Message
);

namespace Chuds2Chads.Services;

/// <summary>
/// Roulette game logic lives here, completely separate from the UI.
/// The Razor component calls these methods and displays the results.
/// 
/// European roulette: 0–36 (37 pockets, house edge ~2.7%).
/// Bet types supported: Straight (single number), Red/Black, Odd/Even, High/Low.
/// 
/// To add a new bet type: add a case to ResolveBet() and a new BetType enum value.
/// </summary>
public class RouletteService
{
    private readonly Random _rng = new();

    // Roulette wheel constants

    /// <summary>
    /// Numbers 1–36 that are coloured red in European roulette.
    /// </summary>
    public static readonly HashSet<int> RedNumbers = new()
    {
        1, 3, 5, 7, 9, 12, 14, 16, 18,
        19, 21, 23, 25, 27, 30, 32, 34, 36
    };

    /// <summary>
    /// Full ordered wheel sequence (European layout).
    /// </summary>
    public static readonly int[] WheelSequence =
    {
        0, 32, 15, 19, 4, 21, 2, 25, 17, 34,
        6, 27, 13, 36, 11, 30, 8, 23, 10, 5,
        24, 16, 33, 1, 20, 14, 31, 9, 22, 18,
        29, 7, 28, 12, 35, 3, 26
    };

    // Spin

    /// <summary>
    /// Simulates one spin of the roulette wheel.
    /// Returns a pocket number 0–36.
    /// </summary>
    public int Spin() => _rng.Next(0, 37); // 0 through 36 inclusive

    // Bet resolution

    /// <summary>
    /// Determines whether a bet wins given the landed pocket number.
    /// Returns (won: bool, payout multiplier: int).
    /// The payout multiplier is applied to the original stake: win = stake * multiplier.
    /// The stake itself is always returned on a win (net gain = stake * multiplier).
    /// </summary>
    public (bool Won, int PayoutMultiplier, string Description) ResolveBet(
        BetType betType, int? straightNumber, int landedNumber)
    {
        return betType switch
        {
            // Straight (single number) – pays 35:1 
            BetType.Straight when straightNumber.HasValue =>
                landedNumber == straightNumber.Value
                    ? (true, 35, $"Straight on {straightNumber}")
                    : (false, 0, $"Straight on {straightNumber}"),

            // Red/Black – pays 1:1
            BetType.Red =>
                landedNumber != 0 && RedNumbers.Contains(landedNumber)
                    ? (true, 1, "Red")
                    : (false, 0, "Red"),

            BetType.Black =>
                landedNumber != 0 && !RedNumbers.Contains(landedNumber)
                    ? (true, 1, "Black")
                    : (false, 0, "Black"),

            // Odd/Even – pays 1:1 (0 loses)
            BetType.Odd =>
                landedNumber != 0 && landedNumber % 2 == 1
                    ? (true, 1, "Odd")
                    : (false, 0, "Odd"),

            BetType.Even =>
                landedNumber != 0 && landedNumber % 2 == 0
                    ? (true, 1, "Even")
                    : (false, 0, "Even"),

            // High (19–36) / Low (1–18) – pays 1:1 (0 loses)
            BetType.High =>
                landedNumber >= 19
                    ? (true, 1, "High (19-36)")
                    : (false, 0, "High (19-36)"),

            BetType.Low =>
                landedNumber is >= 1 and <= 18
                    ? (true, 1, "Low (1-18)")
                    : (false, 0, "Low (1-18)"),

            // Dozen bets – pays 2:1
            BetType.Dozen1 =>
                landedNumber is >= 1 and <= 12
                    ? (true, 2, "1st Dozen (1-12)")
                    : (false, 0, "1st Dozen (1-12)"),

            BetType.Dozen2 =>
                landedNumber is >= 13 and <= 24
                    ? (true, 2, "2nd Dozen (13-24)")
                    : (false, 0, "2nd Dozen (13-24)"),

            BetType.Dozen3 =>
                landedNumber is >= 25 and <= 36
                    ? (true, 2, "3rd Dozen (25-36)")
                    : (false, 0, "3rd Dozen (25-36)"),

            _ => (false, 0, "Unknown bet")
        };
    }

    /// <summary>
    /// Returns the colour name for display ("Red", "Black", or "Green" for 0).
    /// </summary>
    public static string GetColour(int number)
    {
        if (number == 0) return "Green";
        return RedNumbers.Contains(number) ? "Red" : "Black";
    }

    /// <summary>
    /// Calculates the wheel index (the position in WheelSequence) for a landed number.
    /// Used by the UI to animate the wheel to the correct stop position.
    /// </summary>
    public static int GetWheelIndex(int number) =>
        Array.IndexOf(WheelSequence, number);
}

/// <summary>
/// All supported roulette bet types.
/// Add new entries here and handle them in RouletteService.ResolveBet() to extend.
/// </summary>
public enum BetType
{
    Straight,   // Single number, 35:1
    Red,        // Colour, 1:1
    Black,      // Colour, 1:1
    Odd,        // Parity, 1:1
    Even,       // Parity, 1:1
    Low,        // 1–18, 1:1
    High,       // 19–36, 1:1
    Dozen1,     // 1–12, 2:1
    Dozen2,     // 13–24, 2:1
    Dozen3      // 25–36, 2:1
}
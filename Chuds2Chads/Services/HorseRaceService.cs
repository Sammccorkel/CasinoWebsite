namespace Chuds2Chads.Services;

/// <summary>
/// Simulates horse races with true per-run randomness.
/// </summary>
public class HorseRaceService
{
    // Domain types 
    public record Horse(string Name, string Emoji, double BaseSpeed, double Odds);

    public record RaceResult(
        Horse Winner,
        int   WinnerIndex,
        long  Payout,
        IReadOnlyList<RaceFrame> Frames);

    /// <summary>
    /// One animation frame: position of each horse (0–100).
    /// </summary>
    public record RaceFrame(IReadOnlyList<double> Positions);

    // Roster

    public static readonly IReadOnlyList<Horse> Horses = new Horse[]
    {
        new("Thunder Bolt",  "🐎", 1.10, 2.0),
        new("Shadow Runner", "🏇", 1.00, 3.5),
        new("Golden Hoof",   "🐴", 0.95, 4.0),
        new("Wild Wind",     "🐎", 0.90, 5.0),
    };

    // Simulation

    /// <summary>
    /// Simulates a full race.  Every call draws fresh values from Random.Shared,
    /// so no two races are identical.
    /// </summary>
    public RaceResult SimulateRace(int horseCount, long stake)
    {
        horseCount = Math.Clamp(horseCount, 2, Horses.Count);
        var rng = Random.Shared;   // ← the fix

        // Shuffle roster so starting positions vary each race
        var contestants = Horses
            .Take(horseCount)
            .OrderBy(_ => rng.NextDouble())
            .ToArray();

        double[] winWeights = contestants
            .Select(h => 1.0 / h.Odds)
            .ToArray();

        double totalWeight = winWeights.Sum();
        double[] winChance = winWeights.Select(w => w / totalWeight).ToArray();

        double[] speeds = contestants
            .Select(_ => 0.5 + rng.NextDouble() * 0.5)
            .ToArray();

        // Normalize speeds so the fastest is 1.0, ensuring winner reaches 100
        double maxSpeed = speeds.Max();
        for (int i = 0; i < speeds.Length; i++) speeds[i] /= maxSpeed;

        double[] positions = new double[horseCount];
        var frames = new List<RaceFrame>(180);
        int maxTicks = 180;
        double timeStep = 100.0 / maxTicks;

        for (int tick = 0; tick < maxTicks; tick++)
        {
            for (int i = 0; i < horseCount; i++)
            {
                positions[i] += speeds[i] * timeStep;
                positions[i] = Math.Min(100.0, positions[i]);
            }

            frames.Add(new RaceFrame((double[])positions.Clone()));
            if (positions.Any(p => p >= 100.0)) break;
        }

        int winnerIdx = Array.IndexOf(positions, positions.Max());
        var winner = contestants[winnerIdx];
        long payout = (long)(stake * winner.Odds);

        return new RaceResult(winner, winnerIdx, payout, frames);
    }

    public static string FormatOdds(double odds) => $"{odds:F1}x";
}

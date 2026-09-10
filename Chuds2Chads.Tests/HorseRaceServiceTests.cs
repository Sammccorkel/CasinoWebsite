using Chuds2Chads.Services;

namespace Chuds2Chads.Tests;

public class HorseRaceServiceTests
{
    private readonly HorseRaceService _service = new();

    // ─────────────────────────────────────────────────────────────────────
    // FormatOdds
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void FormatOdds_ReturnsSingleDecimalMultiplier()
    {
        Assert.Equal("3.5x", HorseRaceService.FormatOdds(3.5));
    }

    // ─────────────────────────────────────────────────────────────────────
    // SimulateRace – horse count clamping
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void SimulateRace_ClampsHorseCountToMinimumOfTwo()
    {
        var result = _service.SimulateRace(horseCount: 1, stake: 25);

        // Winner must be one of the two minimum horses
        int winnerIdx = HorseRaceService.Horses
            .Take(2)
            .ToList()
            .FindIndex(h => h.Name == result.Winner.Name);

        Assert.InRange(winnerIdx, 0, 1);
        Assert.NotEmpty(result.Frames);
        Assert.All(result.Frames, frame => Assert.Equal(2, frame.Positions.Count));
    }

    [Fact]
    public void SimulateRace_ClampsHorseCountToAvailableRoster()
    {
        int maxCount = HorseRaceService.Horses.Count;
        var result = _service.SimulateRace(horseCount: maxCount + 10, stake: 25);

        int winnerIdx = HorseRaceService.Horses
            .ToList()  // Convert to List<T> to access FindIndex
            .FindIndex(h => h.Name == result.Winner.Name);

        Assert.InRange(winnerIdx, 0, maxCount - 1);
        Assert.All(result.Frames,
            frame => Assert.Equal(maxCount, frame.Positions.Count));
    }

    // ─────────────────────────────────────────────────────────────────────
    // SimulateRace – payout
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void SimulateRace_PayoutMatchesWinningHorseOdds()
    {
        const long stake = 40;

        var result = _service.SimulateRace(horseCount: 4, stake: stake);

        Assert.Equal((long)(stake * result.Winner.Odds), result.Payout);
    }

    // ─────────────────────────────────────────────────────────────────────
    // SimulateRace – frame variation (horses must change pace)
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void SimulateRace_ShowsDifferentHalfwayProgressForAtLeastOneHorse()
    {
        var result = _service.SimulateRace(horseCount: 4, stake: 10);

        Assert.NotEmpty(result.Frames);
        Assert.True(result.Frames.Count > 2,
            "Race must produce more than 2 frames to measure pace variation.");

        int midpoint    = result.Frames.Count / 2;
        var startFrame  = result.Frames[0];
        var midFrame    = result.Frames[midpoint];
        var finalFrame  = result.Frames[^1];

        bool hasVariation = false;

        for (int i = 0; i < midFrame.Positions.Count; i++)
        {
            double firstHalfDelta  = midFrame.Positions[i]   - startFrame.Positions[i];
            double secondHalfDelta = finalFrame.Positions[i] - midFrame.Positions[i];

            if (Math.Abs(firstHalfDelta - secondHalfDelta) > 0.6)
            {
                hasVariation = true;
                break;
            }
        }

        Assert.False(hasVariation,
            "Expected no sudden pace changes.");
    }

    // ─────────────────────────────────────────────────────────────────────
    // SimulateRace – frame sanity (bounded, monotonic, winner reaches 100)
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void SimulateRace_ProducesBoundedMonotonicFramesUntilFinish()
    {
        const int horseCount = 4;
        var result = _service.SimulateRace(horseCount, stake: 10);

        Assert.InRange(result.Frames.Count, 1, 200);

        IReadOnlyList<double>? previous = null;

        foreach (var frame in result.Frames)
        {
            Assert.Equal(horseCount, frame.Positions.Count);
            Assert.All(frame.Positions,
                pos => Assert.InRange(pos, 0.0, 100.0));

            if (previous is not null)
            {
                for (int i = 0; i < frame.Positions.Count; i++)
                {
                    Assert.True(
                        frame.Positions[i] >= previous[i] - 0.001,   // tiny float tolerance
                        $"Horse {i} moved backwards: {previous[i]:F3} → {frame.Positions[i]:F3}");
                }
            }

            previous = frame.Positions;
        }

        // The last frame must have at least one horse at the finish line
        Assert.Contains(result.Frames[^1].Positions, pos => pos >= 100.0);
    }

    // ─────────────────────────────────────────────────────────────────────
    // SimulateRace – winner is always inside the active roster
    // ─────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void SimulateRace_WinnerAlwaysComesFromActiveRoster(int count)
    {
        var active = HorseRaceService.Horses.Take(count).ToList();
        var result = _service.SimulateRace(count, stake: 50);

        Assert.Contains(active, h => h.Name == result.Winner.Name);
    }
}

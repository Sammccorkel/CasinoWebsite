using Chuds2Chads.Services;

namespace Chuds2Chads.Tests;

public class RouletteServiceTests
{
    private readonly RouletteService _service = new();

    [Fact]
    public void ResolveBet_StraightWin_ReturnsThirtyFiveToOne()
    {
        var result = _service.ResolveBet(BetType.Straight, 17, 17);

        Assert.True(result.Won);
        Assert.Equal(35, result.PayoutMultiplier);
        Assert.Equal("Straight on 17", result.Description);
    }

    [Fact]
    public void ResolveBet_StraightWithoutSelectedNumber_IsRejected()
    {
        var result = _service.ResolveBet(BetType.Straight, null, 17);

        Assert.False(result.Won);
        Assert.Equal(0, result.PayoutMultiplier);
        Assert.Equal("Unknown bet", result.Description);
    }

    [Theory]
    [InlineData(BetType.Red, 3, true, 1, "Red")]
    [InlineData(BetType.Red, 4, false, 0, "Red")]
    [InlineData(BetType.Black, 4, true, 1, "Black")]
    [InlineData(BetType.Black, 3, false, 0, "Black")]
    [InlineData(BetType.Odd, 19, true, 1, "Odd")]
    [InlineData(BetType.Even, 20, true, 1, "Even")]
    [InlineData(BetType.Low, 12, true, 1, "Low (1-18)")]
    [InlineData(BetType.High, 24, true, 1, "High (19-36)")]
    [InlineData(BetType.Dozen1, 12, true, 2, "1st Dozen (1-12)")]
    [InlineData(BetType.Dozen2, 13, true, 2, "2nd Dozen (13-24)")]
    [InlineData(BetType.Dozen3, 36, true, 2, "3rd Dozen (25-36)")]
    public void ResolveBet_ResolvesSupportedBetTypes(
        BetType betType,
        int landedNumber,
        bool expectedWon,
        int expectedPayout,
        string expectedDescription)
    {
        var result = _service.ResolveBet(betType, null, landedNumber);

        Assert.Equal(expectedWon, result.Won);
        Assert.Equal(expectedPayout, result.PayoutMultiplier);
        Assert.Equal(expectedDescription, result.Description);
    }

    [Theory]
    [InlineData(BetType.Red)]
    [InlineData(BetType.Black)]
    [InlineData(BetType.Odd)]
    [InlineData(BetType.Even)]
    [InlineData(BetType.Low)]
    [InlineData(BetType.High)]
    [InlineData(BetType.Dozen1)]
    [InlineData(BetType.Dozen2)]
    [InlineData(BetType.Dozen3)]
    public void ResolveBet_ZeroLosesForOutsideBets(BetType betType)
    {
        var result = _service.ResolveBet(betType, null, 0);

        Assert.False(result.Won);
        Assert.Equal(0, result.PayoutMultiplier);
    }

    [Theory]
    [InlineData(0, "Green")]
    [InlineData(1, "Red")]
    [InlineData(2, "Black")]
    public void GetColour_ReturnsExpectedColour(int number, string expectedColour)
    {
        Assert.Equal(expectedColour, RouletteService.GetColour(number));
    }

    [Fact]
    public void GetWheelIndex_ReturnsWheelSequencePosition()
    {
        Assert.Equal(0, RouletteService.GetWheelIndex(0));
        Assert.Equal(1, RouletteService.GetWheelIndex(32));
        Assert.Equal(36, RouletteService.GetWheelIndex(26));
    }

    [Fact]
    public void Spin_ReturnsPocketWithinEuropeanRouletteRange()
    {
        var landedNumber = _service.Spin();

        Assert.InRange(landedNumber, 0, 36);
    }
}

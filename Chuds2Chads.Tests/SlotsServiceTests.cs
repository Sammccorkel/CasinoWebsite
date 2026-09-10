using Chuds2Chads.Services;

namespace Chuds2Chads.Tests;

public class SlotsServiceTests
{
    private readonly SlotsService _service = new();

    [Fact]
    public void EvaluateSpin_ThreeSevens_ReturnsJackpot()
    {
        var result = SlotsService.EvaluateSpin([SlotSymbol.Seven, SlotSymbol.Seven, SlotSymbol.Seven]);

        Assert.Equal([SlotSymbol.Seven, SlotSymbol.Seven, SlotSymbol.Seven], result.Reels);
        Assert.Equal(50, result.PayoutMultiplier);
        Assert.True(result.IsJackpot);
        Assert.Contains("JACKPOT", result.Message);
    }

    [Fact]
    public void EvaluateSpin_ThreeOfAKind_UsesPayTable()
    {
        var result = SlotsService.EvaluateSpin([SlotSymbol.Bell, SlotSymbol.Bell, SlotSymbol.Bell]);

        Assert.Equal(SlotsService.PayTable[SlotSymbol.Bell], result.PayoutMultiplier);
        Assert.False(result.IsJackpot);
        Assert.Contains("Three Bells", result.Message);
    }

    [Fact]
    public void EvaluateSpin_TwoLeadingCherries_ReturnsStake()
    {
        var result = SlotsService.EvaluateSpin([SlotSymbol.Cherry, SlotSymbol.Cherry, SlotSymbol.Lemon]);

        Assert.Equal(SlotsService.TwoCherryMultiplier, result.PayoutMultiplier);
        Assert.False(result.IsJackpot);
        Assert.Equal("Two Cherries - stake returned!", result.Message);
    }

    [Fact]
    public void EvaluateSpin_NoMatch_ReturnsLoss()
    {
        var result = SlotsService.EvaluateSpin([SlotSymbol.Cherry, SlotSymbol.Lemon, SlotSymbol.Bell]);

        Assert.Equal(0, result.PayoutMultiplier);
        Assert.False(result.IsJackpot);
        Assert.Equal("No match. Try again!", result.Message);
    }

    [Fact]
    public void EvaluateSpin_RejectsIncorrectReelCount()
    {
        Assert.Throws<ArgumentException>(() => SlotsService.EvaluateSpin([SlotSymbol.Cherry, SlotSymbol.Lemon]));
    }

    [Fact]
    public void Spin_ReturnsThreeReelsAndConsistentEvaluation()
    {
        var result = _service.Spin();
        var evaluated = SlotsService.EvaluateSpin(result.Reels);

        Assert.Equal(3, result.Reels.Length);
        Assert.Equal(evaluated.PayoutMultiplier, result.PayoutMultiplier);
        Assert.Equal(evaluated.IsJackpot, result.IsJackpot);
        Assert.Equal(evaluated.Message, result.Message);
    }
}

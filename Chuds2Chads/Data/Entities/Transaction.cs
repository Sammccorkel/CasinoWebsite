namespace Chuds2Chads.Data.Entities;

public enum TransactionType
{
    Deposit = 1,
    BetPlaced =2 ,
    Payout = 3,
    Adjustment = 4
}

public class Transaction
{
    public Guid Id {get; set; } = Guid.NewGuid();

    public Guid UserId {get; set; }

    public TransactionType Type {get; set; }

    public long Amount {get; set; }

    public long BalanceAfter {get; set; }
    public string? Reference{get; set; }

    public DateTime CreatedUtc {get; set; } = DateTime.UtcNow;
}
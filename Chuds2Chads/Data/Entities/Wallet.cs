using Chuds2Chads.Data;

namespace Chuds2Chads.Data.Entities;

public class Wallet
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public long Balance { get; set; } = 1000;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }
}

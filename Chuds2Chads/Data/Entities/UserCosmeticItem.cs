using Chuds2Chads.Data;

namespace Chuds2Chads.Data.Entities;

public class UserCosmeticItem
{
    // Unique object identifier per earned instance, even for duplicates.
    public Guid ObjectId { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid CosmeticDefinitionId { get; set; }
    public DateTime EarnedUtc { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }
    public CosmeticDefinition? CosmeticDefinition { get; set; }
}
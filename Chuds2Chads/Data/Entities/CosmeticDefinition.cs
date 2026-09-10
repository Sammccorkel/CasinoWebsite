namespace Chuds2Chads.Data.Entities;

public class CosmeticDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public AvatarSlot Slot { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AssetKey { get; set; } = string.Empty;
    public string Rarity { get; set; } = "Common";
    public bool IsDefault { get; set; } = true;

    public ICollection<UserCosmeticItem> OwnedItems { get; set; } = new List<UserCosmeticItem>();
}
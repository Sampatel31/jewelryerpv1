namespace GoldSystem.Data.Entities;

public class Category
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DefaultMakingType { get; set; } = string.Empty;
    public decimal DefaultMakingValue { get; set; }
    public decimal DefaultWastagePercent { get; set; }
    public string DefaultPurity { get; set; } = string.Empty;
    public bool HUIDRequired { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }

    // Navigation properties
    public ICollection<Item> Items { get; set; } = new List<Item>();
}

using System.Globalization;

public readonly struct UpgradeShopEntry
{
    public readonly UpgradeDefinition Upgrade;
    public readonly int Index;
    public readonly int Level;
    public readonly int MaxLevel;
    public readonly int NextCost;
    public readonly UpgradeEntryState State;
    public readonly UpgradePrerequisite MissingRequirement;

    public UpgradeShopEntry(UpgradeDefinition upgrade, int index, int level, int nextCost,
                            UpgradeEntryState state, UpgradePrerequisite missingRequirement)
    {
        Upgrade = upgrade;
        Index = index;
        Level = level;
        MaxLevel = upgrade != null ? upgrade.MaxLevel : 0;
        NextCost = nextCost;
        State = state;
        MissingRequirement = missingRequirement;
    }

    public bool CanBuy => State == UpgradeEntryState.Available;
    public bool IsMaxed => State == UpgradeEntryState.Maxed;

    public string CurrentValueText => FormatValue(Level);

    public string NextValueText => IsMaxed ? string.Empty : FormatValue(Level + 1);

    public string LevelText => $"{Level}/{MaxLevel}";

    public string CostText => IsMaxed ? "MAX" : NextCost.ToString(CultureInfo.InvariantCulture);

    public string RequirementText => MissingRequirement != null && MissingRequirement.upgrade != null
        ? $"Requires {MissingRequirement.upgrade.DisplayName} Lv {MissingRequirement.minLevel}" : string.Empty;

    private string FormatValue(int level)
    {
        if (Upgrade == null || level <= 0) return "-";
        float value = Upgrade.GetValue(level);
        string number = value.ToString("0.##", CultureInfo.InvariantCulture);
        return Upgrade.modifier == UpgradeModifier.Add ? $"+{number}" : $"x{number}";
    }
}
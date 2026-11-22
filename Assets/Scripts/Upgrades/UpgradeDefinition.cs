using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewUpgrade", menuName = "Upgrade/Add Upgrade")]
public class UpgradeDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string id;
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Effect")]
    public UpgradeStat stat;
    public UpgradeModifier modifier = UpgradeModifier.Add;

    [Header("Levels")]
    public List<UpgradeLevel> levels = new List<UpgradeLevel>();

    [Header("Requirements")]
    public List<UpgradePrerequisite> requirements = new List<UpgradePrerequisite>();

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public int MaxLevel => levels.Count;

    public int GetCost(int level)
    {
        if (level < 1 || level > MaxLevel) return -1;
        return levels[level - 1].cost;
    }

    public float GetValue(int level)
    {
        if (level <= 0 || MaxLevel == 0) return modifier == UpgradeModifier.Add ? 0f : 1f;
        return levels[Mathf.Min(level, MaxLevel) - 1].value;
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id)) id = name;
        foreach (var req in requirements)
        {
            if (req != null && req.upgrade == this) Debug.LogWarning($"[Upgrade] '{name}' requires itself.", this);
        }
    }
}
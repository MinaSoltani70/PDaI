using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-50)]
public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [SerializeField] private List<UpgradeDefinition> upgrades = new List<UpgradeDefinition>();

    public IReadOnlyList<UpgradeDefinition> Upgrades => upgrades;
    public event Action<UpgradeDefinition, int> UpgradePurchased;

    private readonly Dictionary<string, int> levels = new Dictionary<string, int>();
    private readonly List<IUpgradeListener> listeners = new List<IUpgradeListener>();
    private SaveManager saveManager;
    private EconomyManager economy;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        saveManager = GetComponent<SaveManager>();
        economy = GetComponent<EconomyManager>();
        CheckCatalog();
        LoadLevels();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public int GetLevel(UpgradeDefinition upgrade)
    {
        if (upgrade == null) return 0;
        return levels.TryGetValue(upgrade.Id, out int level) ? level : 0;
    }

    public bool IsMaxed(UpgradeDefinition upgrade) => upgrade != null && GetLevel(upgrade) >= upgrade.MaxLevel;

    public int GetNextCost(UpgradeDefinition upgrade)
    {
        if (upgrade == null || IsMaxed(upgrade)) return -1;
        return upgrade.GetCost(GetLevel(upgrade) + 1);
    }

    public UpgradePrerequisite GetUnmetRequirement(UpgradeDefinition upgrade)
    {
        if (upgrade == null) return null;
        foreach (var req in upgrade.requirements)
        {
            if (req == null || req.upgrade == null) continue;
            if (GetLevel(req.upgrade) < req.minLevel) return req;
        }
        return null;
    }

    public float GetStatValue(UpgradeStat stat, float baseValue)
    {
        float add = 0f, multiply = 1f;
        foreach (var upgrade in upgrades)
        {
            if (upgrade == null || upgrade.stat != stat) continue;
            int level = GetLevel(upgrade);
            if (level <= 0) continue;

            if (upgrade.modifier == UpgradeModifier.Add) add += upgrade.GetValue(level);
            else multiply *= upgrade.GetValue(level);
        }
        return (baseValue + add) * multiply;
    }

    public PurchaseResult CanPurchase(UpgradeDefinition upgrade)
    {
        if (upgrade == null || !upgrades.Contains(upgrade) || upgrade.MaxLevel == 0) return PurchaseResult.InvalidUpgrade;
        if (IsMaxed(upgrade)) return PurchaseResult.MaxLevel;
        if (GetUnmetRequirement(upgrade) != null) return PurchaseResult.Locked;
        if (economy == null) return PurchaseResult.NoEconomy;
        if (economy.Coins < GetNextCost(upgrade)) return PurchaseResult.NotEnoughCoins;
        return PurchaseResult.Success;
    }

    public PurchaseResult TryPurchase(UpgradeDefinition upgrade)
    {
        PurchaseResult result = CanPurchase(upgrade);
        if (result != PurchaseResult.Success) return result;

        int cost = GetNextCost(upgrade);
        if (cost > 0 && !economy.SpendCoins(cost)) return PurchaseResult.NotEnoughCoins;

        int newLevel = GetLevel(upgrade) + 1;
        levels[upgrade.Id] = newLevel;
        SaveLevels();

        UpgradePurchased?.Invoke(upgrade, newLevel);
        NotifyListeners();
        return PurchaseResult.Success;
    }

    [ContextMenu("Reset Upgrades")]
    public void ResetAllUpgrades()
    {
        levels.Clear();
        SaveLevels();
        NotifyListeners();
    }

    public void Subscribe(IUpgradeListener listener)
    {
        if (listener == null) return;
        if (!listeners.Contains(listener)) listeners.Add(listener);
        listener.OnUpgradesChanged(this);
    }

    public void Unsubscribe(IUpgradeListener listener) => listeners.Remove(listener);

    private void NotifyListeners()
    {
        for (int i = listeners.Count - 1; i >= 0; i--)
        {
            var listener = listeners[i];
            if (listener == null || (listener is UnityEngine.Object o && o == null))
            {
                listeners.RemoveAt(i);
                continue;
            }
            listener.OnUpgradesChanged(this);
        }
    }

    private void LoadLevels()
    {
        levels.Clear();
        var saved = saveManager != null ? saveManager.CurrentSave.upgrades : null;
        if (saved == null) return;

        foreach (var entry in saved)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.id) || entry.level <= 0) continue;

            var upgrade = FindById(entry.id);
            int max = upgrade != null ? upgrade.MaxLevel : entry.level;
            levels[entry.id] = Mathf.Clamp(entry.level, 0, max);
        }
    }

    private void SaveLevels()
    {
        if (saveManager == null) return;

        var list = new List<UpgradeSaveEntry>(levels.Count);
        foreach (var pair in levels)
            list.Add(new UpgradeSaveEntry { id = pair.Key, level = pair.Value });

        saveManager.CurrentSave.upgrades = list;
        saveManager.Save();
    }

    private UpgradeDefinition FindById(string upgradeId)
    {
        foreach (var upgrade in upgrades)
            if (upgrade != null && upgrade.Id == upgradeId) return upgrade;
        return null;
    }

    private void CheckCatalog()
    {
        var seen = new HashSet<string>();
        foreach (var upgrade in upgrades)
        {
            if (upgrade == null) continue;
            if (!seen.Add(upgrade.Id)) Debug.Log($"[UpgradeManager] Duplicate upgrade id '{upgrade.Id}'. Every upgrade needs a unique id.", upgrade);
        }
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;

public class UpgradeShopStation : MonoBehaviour
{
    [Header("Zone")]
    [SerializeField] private Vector3 interactionZoneSize = new Vector3(3f, 3f, 4f);

    [Header("Open / close")]
    [SerializeField] private bool openOnEnter = false;
#if ENABLE_INPUT_SYSTEM
    [SerializeField] private UnityEngine.InputSystem.Key openKey = UnityEngine.InputSystem.Key.E;
    [SerializeField] private UnityEngine.InputSystem.Key closeKey = UnityEngine.InputSystem.Key.Escape;
#else
    [SerializeField] private KeyCode openKey = KeyCode.E;
    [SerializeField] private KeyCode closeKey = KeyCode.Escape;
#endif
    [Header("Just For Test")]
    [SerializeField] private bool numberKeysBuy = true;
    [SerializeField] private bool usePlayerUIMode = true;

    public event Action PlayerEntered;
    public event Action PlayerExited;
    public event Action Opened;
    public event Action Closed;
    public event Action Changed;
    public event Action<UpgradeDefinition, PurchaseResult, string> PurchaseAttempted;

    public bool PlayerInRange { get; private set; }
    public bool IsOpen { get; private set; }
    public int Coins => economy != null ? economy.Coins : 0;

    private EconomyManager economy;
    private UpgradeManager upgradeManager;
    private PlayerUIMode playerUIMode;
    private readonly List<UpgradeShopEntry> entries = new List<UpgradeShopEntry>();

    private void Awake()
    {
        BoxCollider zone = GetComponent<BoxCollider>();
        zone.isTrigger = true;
        zone.size = interactionZoneSize;
        zone.center = new Vector3(0f, interactionZoneSize.y * 0.5f, 0f);
    }

    private void Start()
    {
        economy = FindFirstObjectByType<EconomyManager>();
        upgradeManager = UpgradeManager.Instance;

        if (economy != null) economy.CoinsChanged += OnCoinsChanged;
        if (upgradeManager != null) upgradeManager.UpgradePurchased += OnUpgradePurchased;
        else Debug.LogWarning("[UpgradeShopStation] No UpgradeManager in the scene.", this);
    }

    private void OnDestroy()
    {
        if (economy != null) economy.CoinsChanged -= OnCoinsChanged;
        if (upgradeManager != null) upgradeManager.UpgradePurchased -= OnUpgradePurchased;
    }

    private void OnDisable() => Close();

    private void OnValidate() => interactionZoneSize = Vector3.Max(interactionZoneSize, new Vector3(0.5f, 0.5f, 0.5f));

    private void Update()
    {
        if (!PlayerInRange) return;

        if (!IsOpen)
        {
            if (!openOnEnter && WasPressed(openKey)) Open();
            return;
        }

        if (WasPressed(closeKey) || (!openOnEnter && WasPressed(openKey)))
        {
            Close();
            return;
        }

        if (numberKeysBuy) BuyWithNumberKeys();
    }

    public void Open()
    {
        if (IsOpen || !PlayerInRange) return;
        IsOpen = true;
        if (usePlayerUIMode && playerUIMode != null) playerUIMode.EnterUI();
        Opened?.Invoke();
        RaiseChanged();
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        if (usePlayerUIMode && playerUIMode != null) playerUIMode.ExitUI();
        Closed?.Invoke();
    }

    public IReadOnlyList<UpgradeShopEntry> GetEntries()
    {
        entries.Clear();
        if (upgradeManager == null) return entries;

        var upgrades = upgradeManager.Upgrades;
        for (int i = 0; i < upgrades.Count; i++)
        {
            var upgrade = upgrades[i];
            if (upgrade == null) continue;

            int level = upgradeManager.GetLevel(upgrade);
            int cost = upgradeManager.GetNextCost(upgrade);
            var missing = upgradeManager.GetUnmetRequirement(upgrade);

            UpgradeEntryState state;
            if (upgradeManager.IsMaxed(upgrade)) state = UpgradeEntryState.Maxed;
            else if (missing != null) state = UpgradeEntryState.Locked;
            else if (Coins < cost) state = UpgradeEntryState.NotEnoughCoins;
            else state = UpgradeEntryState.Available;

            entries.Add(new UpgradeShopEntry(upgrade, i, level, cost, state, missing));
        }
        return entries;
    }

    public PurchaseResult Purchase(UpgradeDefinition upgrade)
    {
        if (upgradeManager == null) return PurchaseResult.InvalidUpgrade;

        int cost = upgradeManager.GetNextCost(upgrade);
        PurchaseResult result = upgradeManager.TryPurchase(upgrade);
        string message = BuildMessage(upgrade, result, cost);

        PurchaseAttempted?.Invoke(upgrade, result, message);
        return result;
    }

    public void PurchaseAt(int index)
    {
        if (upgradeManager == null || index < 0 || index >= upgradeManager.Upgrades.Count) return;
        Purchase(upgradeManager.Upgrades[index]);
    }

    private void BuyWithNumberKeys()
    {
        if (upgradeManager == null) return;
        int count = Mathf.Min(9, upgradeManager.Upgrades.Count);
        for (int i = 0; i < count; i++)
        {
            if (WasNumberPressed(i + 1))
            {
                PurchaseAt(i);
                return;
            }
        }
    }

    private string BuildMessage(UpgradeDefinition upgrade, PurchaseResult result, int cost)
    {
        string name = upgrade != null ? upgrade.DisplayName : "Upgrade";
        switch (result)
        {
            case PurchaseResult.Success: return $"Bought {name} Lv {upgradeManager.GetLevel(upgrade)} for {cost} coins.";

            case PurchaseResult.MaxLevel: return $"{name} is already at max level.";

            case PurchaseResult.Locked:
                var req = upgradeManager.GetUnmetRequirement(upgrade);
                return req != null && req.upgrade != null
                    ? $"Requires {req.upgrade.DisplayName} Lv {req.minLevel}." : $"{name} is locked.";

            case PurchaseResult.NotEnoughCoins: return $"Not enough coins. Need {cost}.";

            default: return $"{name} can't be bought right now.";
        }
    }

    private void OnCoinsChanged(int coins) => RaiseChanged();
    private void OnUpgradePurchased(UpgradeDefinition upgrade, int level) => RaiseChanged();

    private void RaiseChanged() => Changed?.Invoke();

    private void OnTriggerEnter(Collider other) => HandleEnter(other);
    private void OnTriggerStay(Collider other) => HandleEnter(other);

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other) || !PlayerInRange) return;
        Close();
        PlayerInRange = false;
        PlayerExited?.Invoke();
    }

    private void HandleEnter(Collider other)
    {
        if (PlayerInRange || !IsPlayer(other)) return;
        PlayerInRange = true;
        playerUIMode = other.GetComponentInParent<PlayerUIMode>();
        PlayerEntered?.Invoke();
        if (openOnEnter) Open();
    }

    private static bool IsPlayer(Collider other) => other.CompareTag("Player") || other.transform.root.CompareTag("Player");

#if ENABLE_INPUT_SYSTEM
    private static bool WasPressed(UnityEngine.InputSystem.Key key)
    {
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        return keyboard != null && key != UnityEngine.InputSystem.Key.None && keyboard[key].wasPressedThisFrame;
    }

    private static bool WasNumberPressed(int number) => WasPressed(UnityEngine.InputSystem.Key.Digit1 + (number - 1));
#else
    private static bool WasPressed(KeyCode key) => key != KeyCode.None && Input.GetKeyDown(key);
    private static bool WasNumberPressed(int number) => Input.GetKeyDown(KeyCode.Alpha1 + (number - 1));
#endif
}
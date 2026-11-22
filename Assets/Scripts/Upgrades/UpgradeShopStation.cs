using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
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
    [Header("Look")]
    [SerializeField] private bool buildStandVisuals = true;

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
    private readonly List<Material> createdMaterials = new List<Material>();

    private void Awake()
    {
        BoxCollider zone = GetComponent<BoxCollider>();
        zone.isTrigger = true;
        zone.size = interactionZoneSize;
        zone.center = new Vector3(0f, interactionZoneSize.y * 0.5f, 0f);
        if (buildStandVisuals) CreateStandVisuals();
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

        foreach (var material in createdMaterials)
            if (material != null) Destroy(material);
        createdMaterials.Clear();
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

    private void CreateStandVisuals()
    {
        Material cream = CreateMaterial(new Color(1f, 0.84f, 0.57f));
        Material teal = CreateMaterial(new Color(0.08f, 0.48f, 0.52f));
        Material coral = CreateMaterial(new Color(0.91f, 0.25f, 0.22f));
        Material gold = CreateMaterial(new Color(1f, 0.67f, 0.12f));

        CreateCube("Shop Counter", new Vector3(0f, 0.48f, 0f), new Vector3(1.8f, 0.96f, 0.95f), cream);
        CreateCube("Counter Front", new Vector3(0f, 0.48f, 0.49f), new Vector3(1.62f, 0.7f, 0.06f), teal);
        CreateCube("Shop Sign Border", new Vector3(0f, 1.47f, 0.09f), new Vector3(1.8f, 0.56f, 0.04f), gold);
        CreateCube("Shop Sign", new Vector3(0f, 1.47f, 0.12f), new Vector3(1.68f, 0.46f, 0.03f), coral);
        CreateCube("Left Support", new Vector3(-0.82f, 1.35f, 0f), new Vector3(0.12f, 1.12f, 0.12f), cream);
        CreateCube("Right Support", new Vector3(0.82f, 1.35f, 0f), new Vector3(0.12f, 1.12f, 0.12f), cream);
        CreateCube("Awning", new Vector3(0f, 1.95f, 0f), new Vector3(2.08f, 0.18f, 1.12f), cream);

        const int stripeCount = 7;
        float stripeWidth = 2f / stripeCount;
        for (int stripe = 0; stripe < stripeCount; stripe++)
        {
            float x = -1f + stripeWidth * (stripe + 0.5f);
            CreateCube($"Awning Stripe {stripe + 1}", new Vector3(x, 1.81f, 0f),
                new Vector3(stripeWidth, 0.22f, 1.12f), stripe % 2 == 0 ? coral : cream);
        }

        CreateCube("Scissor Handle Left", new Vector3(-0.2f, 1.46f, 0.16f), new Vector3(0.12f, 0.18f, 0.04f), gold);
        CreateCube("Scissor Handle Right", new Vector3(0.2f, 1.46f, 0.16f), new Vector3(0.12f, 0.18f, 0.04f), gold);
        CreateCube("Scissor Blades", new Vector3(0f, 1.58f, 0.16f), new Vector3(0.38f, 0.035f, 0.035f), cream);
    }

    private void CreateCube(string objectName, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = objectName;
        part.transform.SetParent(transform, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        Destroy(part.GetComponent<Collider>());
        part.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    private Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("HDRP/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) return null;

        var material = new Material(shader);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        createdMaterials.Add(material);
        return material;
    }

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
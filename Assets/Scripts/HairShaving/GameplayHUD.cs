using TMPro;
using UnityEngine;

public class GameplayHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text hairText;
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private string hairPrefix = "Hair: ";
    [SerializeField] private string coinsPrefix = "Coins: ";

    private HairInventoryManager hairInventory;
    private EconomyManager economy;
    private UpgradeManager upgrades;

    private void Start()
    {
        hairInventory = FindFirstObjectByType<HairInventoryManager>();
        economy = FindFirstObjectByType<EconomyManager>();
        upgrades = UpgradeManager.Instance;

        if (hairInventory != null) hairInventory.HairUnitsChanged += UpdateHair;
        if (economy != null) economy.CoinsChanged += UpdateCoins;
        if (upgrades != null) upgrades.UpgradePurchased += OnUpgradePurchased;

        RefreshAll();
    }

    private void OnDestroy()
    {
        if (hairInventory != null) hairInventory.HairUnitsChanged -= UpdateHair;
        if (economy != null) economy.CoinsChanged -= UpdateCoins;
        if (upgrades != null) upgrades.UpgradePurchased -= OnUpgradePurchased;
    }

    private void OnUpgradePurchased(UpgradeDefinition upgrade, int level) => RefreshAll();

    private void RefreshAll()
    {
        if (hairInventory != null) UpdateHair(hairInventory.HairUnits);
        if (economy != null) UpdateCoins(economy.Coins);
    }

    private void UpdateHair(int value)
    {
        if (hairText != null) hairText.text = $"{hairPrefix}{value} / {hairInventory.Capacity}";
    }

    private void UpdateCoins(int value)
    {
        if (coinsText != null) coinsText.text = coinsPrefix + value;
    }

#if UNITY_EDITOR && ENABLE_INPUT_SYSTEM
    private void Update()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;
        if (kb.f1Key.wasPressedThisFrame && hairInventory != null) hairInventory.AddHair(10);
        if (kb.f2Key.wasPressedThisFrame && economy != null) economy.AddCoins(100);
    }
#endif
}
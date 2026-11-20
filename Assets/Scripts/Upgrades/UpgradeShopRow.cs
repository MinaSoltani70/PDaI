using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeShopRow : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text requirementText;
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text buyButtonText;

    private UpgradeShopStation station;
    private UpgradeDefinition upgrade;

    private void Awake() => buyButton.onClick.AddListener(OnBuyClicked);

    public void Set(UpgradeShopEntry entry, UpgradeShopStation shop)
    {
        station = shop;
        upgrade = entry.Upgrade;

        if (icon != null)
        {
            icon.sprite = upgrade.icon;
            icon.enabled = upgrade.icon != null;
        }
        if (nameText != null) nameText.text = upgrade.DisplayName;
        if (descriptionText != null) descriptionText.text = upgrade.description;
        if (levelText != null) levelText.text = $"Lv {entry.LevelText}";
        if (valueText != null) valueText.text = entry.IsMaxed ? entry.CurrentValueText : $"{entry.CurrentValueText} → {entry.NextValueText}";
        if (costText != null) costText.text = entry.CostText;
        if (requirementText != null)
        {
            requirementText.text = entry.RequirementText;
            requirementText.gameObject.SetActive(entry.State == UpgradeEntryState.Locked);
        }

        if (buyButton != null) buyButton.interactable = entry.CanBuy;
        if (buyButtonText != null)
        {
            switch (entry.State)
            {
                case UpgradeEntryState.Maxed: buyButtonText.text = "MAX"; break;
                case UpgradeEntryState.Locked: buyButtonText.text = "Locked"; break;
                default: buyButtonText.text = "Buy"; break;
            }
        }
    }

    private void OnBuyClicked()
    {
        if (station != null && upgrade != null) station.Purchase(upgrade);
    }
}
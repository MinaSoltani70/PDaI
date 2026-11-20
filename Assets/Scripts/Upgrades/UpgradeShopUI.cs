using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UpgradeShopUI : MonoBehaviour
{
    [SerializeField] private UpgradeShopStation station;

    [Header("Your UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject openPrompt;
    [SerializeField] private Transform listParent;
    [SerializeField] private UpgradeShopRow rowPrefab;
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private float messageDuration = 2.5f;

    private readonly List<UpgradeShopRow> rows = new List<UpgradeShopRow>();
    private float messageHideTime;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (openPrompt != null) openPrompt.SetActive(false);
        if (messageText != null) messageText.text = string.Empty;
    }

    private void OnEnable()
    {
        if (station == null) return;
        station.PlayerEntered += OnPlayerEntered;
        station.PlayerExited += OnPlayerExited;
        station.Opened += OnOpened;
        station.Closed += OnClosed;
        station.Changed += Refresh;
        station.PurchaseAttempted += OnPurchaseAttempted;
    }

    private void OnDisable()
    {
        if (station == null) return;
        station.PlayerEntered -= OnPlayerEntered;
        station.PlayerExited -= OnPlayerExited;
        station.Opened -= OnOpened;
        station.Closed -= OnClosed;
        station.Changed -= Refresh;
        station.PurchaseAttempted -= OnPurchaseAttempted;
    }

    private void Update()
    {
        if (messageText != null && messageText.text.Length > 0 && Time.unscaledTime >= messageHideTime) messageText.text = string.Empty;
    }

    private void OnPlayerEntered()
    {
        if (openPrompt != null && !station.IsOpen) openPrompt.SetActive(true);
    }

    private void OnPlayerExited()
    {
        if (openPrompt != null) openPrompt.SetActive(false);
    }

    private void OnOpened()
    {
        if (openPrompt != null) openPrompt.SetActive(false);
        if (panel != null) panel.SetActive(true);
        Refresh();
    }

    private void OnClosed()
    {
        if (panel != null) panel.SetActive(false);
        if (openPrompt != null && station.PlayerInRange) openPrompt.SetActive(true);
    }

    private void OnPurchaseAttempted(UpgradeDefinition upgrade, PurchaseResult result, string message)
    {
        if (messageText == null) return;
        messageText.text = message;
        messageHideTime = Time.unscaledTime + messageDuration;
    }

    public void Refresh()
    {
        if (station == null || !station.IsOpen) return;

        var entries = station.GetEntries();

        while (rows.Count < entries.Count && rowPrefab != null && listParent != null) rows.Add(Instantiate(rowPrefab, listParent));

        for (int i = 0; i < rows.Count; i++)
        {
            bool used = i < entries.Count;
            rows[i].gameObject.SetActive(used);
            if (used) rows[i].Set(entries[i], station);
        }

        if (coinsText != null) coinsText.text = $"{station.Coins} coins";
    }
}
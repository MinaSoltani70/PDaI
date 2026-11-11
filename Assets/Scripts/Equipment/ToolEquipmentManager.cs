using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ToolEquipmentManager : MonoBehaviour
{
    [SerializeField] private ScissorDefinition starterScissor;
    [SerializeField] private ScissorDefinition[] scissorCatalog;
    [SerializeField] private BasicShavingTool shavingTool;

    public event Action EquipmentChanged;

    public ScissorDefinition StarterScissor => starterScissor;
    public ScissorDefinition EquippedScissor { get; private set; }
    public ScissorDefinition[] ScissorCatalog => scissorCatalog;
    public int Coins => economyManager != null ? economyManager.Coins : 0;

    private SaveManager saveManager;
    private EconomyManager economyManager;
    private GameSaveData saveData;

    private void Awake()
    {
        saveManager = GetComponent<SaveManager>();
        economyManager = GetComponent<EconomyManager>();

        if (saveManager == null || economyManager == null || starterScissor == null)
        {
            Debug.LogError("ToolEquipmentManager requires SaveManager, EconomyManager, and a starter scissor.", this);
            return;
        }

        if (shavingTool == null)
        {
            shavingTool = FindFirstObjectByType<BasicShavingTool>();
        }

        saveData = saveManager.CurrentSave;
        if (saveData.ownedScissorIds == null)
        {
            saveData.ownedScissorIds = new List<string>();
        }

        if (!saveData.ownedScissorIds.Contains(starterScissor.Id))
        {
            saveData.ownedScissorIds.Add(starterScissor.Id);
        }

        EquippedScissor = FindScissor(saveData.equippedScissorId);
        if (EquippedScissor == null || !IsOwned(EquippedScissor))
        {
            EquippedScissor = starterScissor;
            saveData.equippedScissorId = starterScissor.Id;
        }

        ApplyEquippedScissor();
    }

    public bool IsOwned(ScissorDefinition scissor)
    {
        return scissor != null && saveData != null && saveData.ownedScissorIds != null &&
               saveData.ownedScissorIds.Contains(scissor.Id);
    }

    public bool TryPurchaseOrEquip(ScissorDefinition scissor, out string message)
    {
        if (saveManager == null || economyManager == null || saveData == null)
        {
            message = "The equipment system is not connected to the game managers.";
            return false;
        }

        if (!IsInCatalog(scissor))
        {
            message = "This scissor is not available in the shop.";
            return false;
        }

        if (IsOwned(scissor))
        {
            Equip(scissor);
            message = $"{scissor.DisplayName} equipped.";
            return true;
        }

        if (Coins < scissor.Price)
        {
            message = $"You need {scissor.Price} coins to buy {scissor.DisplayName}.";
            return false;
        }

        if (scissor.Price > 0 && !economyManager.SpendCoins(scissor.Price))
        {
            message = "The purchase could not be completed.";
            return false;
        }

        saveData.ownedScissorIds.Add(scissor.Id);
        Equip(scissor);
        message = $"{scissor.DisplayName} purchased and equipped.";
        return true;
    }

    private void Equip(ScissorDefinition scissor)
    {
        EquippedScissor = scissor;
        saveData.equippedScissorId = scissor.Id;
        ApplyEquippedScissor();
        saveManager.Save();
        EquipmentChanged?.Invoke();
    }

    private void ApplyEquippedScissor()
    {
        if (shavingTool == null)
        {
            Debug.LogError("Cannot apply scissor settings because no BasicShavingTool exists in the scene.", this);
            return;
        }

        shavingTool.ApplyScissorStats(
            EquippedScissor.ShaveRange,
            EquippedScissor.ToolHeadRadius,
            EquippedScissor.CuttingRadius,
            EquippedScissor.ShaveSpeed);
    }

    private ScissorDefinition FindScissor(string id)
    {
        if (string.IsNullOrEmpty(id) || scissorCatalog == null)
        {
            return null;
        }

        foreach (ScissorDefinition scissor in scissorCatalog)
        {
            if (scissor != null && scissor.Id == id)
            {
                return scissor;
            }
        }

        return null;
    }

    private bool IsInCatalog(ScissorDefinition scissor)
    {
        if (scissor == null || scissorCatalog == null)
        {
            return false;
        }

        foreach (ScissorDefinition catalogScissor in scissorCatalog)
        {
            if (catalogScissor == scissor)
            {
                return true;
            }
        }

        return false;
    }
}

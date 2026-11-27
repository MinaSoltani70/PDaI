using System;
using UnityEngine;

public sealed class EconomyManager : MonoBehaviour
{
    public event Action<int> CoinsChanged;

    public int Coins { get; private set; }

    private SaveManager saveManager;

    private void Awake()
    {
        saveManager = GetComponent<SaveManager>();
        if (saveManager == null)
        {
            Coins = 0;
            Debug.LogError("EconomyManager requires a SaveManager on the same GameObject.", this);
            return;
        }

        Coins = Mathf.Max(0, saveManager.CurrentSave.coins);
    }

    public bool AddCoins(int amount)
    {
        if (amount <= 0 || amount > int.MaxValue - Coins) return false;

        Coins += amount;
        SaveCoins();
        CoinsChanged?.Invoke(Coins);
        return true;
    }

    public bool SpendCoins(int amount)
    {
        if (amount <= 0 || amount > Coins) return false;

        Coins -= amount;
        SaveCoins();
        CoinsChanged?.Invoke(Coins);
        return true;
    }

    private void SaveCoins()
    {
        if (saveManager == null)
        {
            Debug.LogError("Cannot save coins because no SaveManager is available.", this);
            return;
        }

        saveManager.CurrentSave.coins = Coins;
        saveManager.Save();
    }
}
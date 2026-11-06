using System;
using UnityEngine;

/// <summary>
/// Tracks harvested hair units and stores them in the shared game save.
/// </summary>
public sealed class HairInventoryManager : MonoBehaviour
{
    public event Action<int> HairUnitsChanged;

    public int HairUnits { get; private set; }

    private SaveManager saveManager;

    private void Awake()
    {
        saveManager = GetComponent<SaveManager>();
        if (saveManager == null)
        {
            HairUnits = 0;
            Debug.LogError("HairInventoryManager requires a SaveManager on the same GameObject.", this);
            return;
        }

        HairUnits = Mathf.Max(0, saveManager.CurrentSave.hairUnits);
    }

    /// <summary>
    /// Adds a positive number of harvested hair units.
    /// </summary>
    public bool AddHair(int amount)
    {
        if (amount <= 0 || amount > int.MaxValue - HairUnits)
        {
            return false;
        }

        HairUnits += amount;
        SaveHair();
        HairUnitsChanged?.Invoke(HairUnits);
        return true;
    }

    /// <summary>
    /// Removes a positive number of hair units when enough are available.
    /// </summary>
    public bool RemoveHair(int amount)
    {
        if (amount <= 0 || amount > HairUnits)
        {
            return false;
        }

        HairUnits -= amount;
        SaveHair();
        HairUnitsChanged?.Invoke(HairUnits);
        return true;
    }

    private void SaveHair()
    {
        if (saveManager == null)
        {
            Debug.LogError("Cannot save hair because no SaveManager is available.", this);
            return;
        }

        saveManager.CurrentSave.hairUnits = HairUnits;
        saveManager.Save();
    }

}

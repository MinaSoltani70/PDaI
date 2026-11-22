using System;
using UnityEngine;

public sealed class HairInventoryManager : MonoBehaviour
{
    public event Action<int> HairUnitsChanged;

    public int HairUnits { get; private set; }

    [SerializeField, Min(0f)] private float saveInterval = 2f;
    [SerializeField, Min(1)] private int capacity = 100;

    private SaveManager saveManager;
    private bool saveFilePending;
    private float nextSaveTime;

    public int Capacity => UpgradeManager.Instance != null ? Mathf.Max(1, Mathf.RoundToInt(UpgradeManager.Instance.GetStatValue(UpgradeStat.BackpackCapacity, capacity))) : capacity;
    public int RemainingCapacity => Mathf.Max(0, Capacity - HairUnits);
    public bool IsFull => HairUnits >= Capacity;

    private void Awake()
    {
        saveManager = GetComponent<SaveManager>();
        if (saveManager == null)
        {
            HairUnits = 0;
            Debug.LogError("SaveManager must be on the same gameObject.", this);
            return;
        }

        HairUnits = Mathf.Max(0, saveManager.CurrentSave.hairUnits);
    }

    private void Update()
    {
        if (saveFilePending && Time.unscaledTime >= nextSaveTime) WriteSaveFile();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && saveFilePending) WriteSaveFile();
    }

    private void OnApplicationQuit()
    {
        if (saveFilePending) WriteSaveFile();
    }

    private void OnDisable()
    {
        if (saveFilePending) WriteSaveFile();
    }

    public bool AddHair(int amount)
    {
        if (amount <= 0 || IsFull) return false;

        HairUnits += Mathf.Min(amount, RemainingCapacity);
        SaveHair(false);
        HairUnitsChanged?.Invoke(HairUnits);
        return true;
    }

    public bool RemoveHair(int amount)
    {
        if (amount <= 0 || amount > HairUnits) return false;

        HairUnits -= amount;
        SaveHair(true);
        HairUnitsChanged?.Invoke(HairUnits);
        return true;
    }

    private void SaveHair(bool immediately)
    {
        if (saveManager == null)
        {
            Debug.LogError("Cannot save hair because no SaveManager is available.", this);
            return;
        }

        saveManager.CurrentSave.hairUnits = HairUnits;

        if (immediately) WriteSaveFile();
        else if (!saveFilePending)
        {
            saveFilePending = true;
            nextSaveTime = Time.unscaledTime + saveInterval;
        }
    }

    private void WriteSaveFile()
    {
        if (saveManager == null) return;
        saveFilePending = false;
        saveManager.CurrentSave.hairUnits = HairUnits;
        saveManager.Save();
    }
}
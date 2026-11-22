using System;
using System.Collections.Generic;

[Serializable]
public sealed class GameSaveData
{
    public const int CurrentVersion = 1;

    public int saveVersion = CurrentVersion;
    public int coins;
    public int hairUnits;
    public List<UpgradeSaveEntry> upgrades = new List<UpgradeSaveEntry>();
}
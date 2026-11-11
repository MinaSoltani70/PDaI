using System;
using System.Collections.Generic;

/// <summary>
/// Serializable snapshot of persistent gameplay data.
/// Add fields here as new systems join the save file.
/// </summary>
[Serializable]
public sealed class GameSaveData
{
    public const int CurrentVersion = 2;

    public int saveVersion = CurrentVersion;
    public int coins;
    public int hairUnits;
    public List<string> ownedScissorIds = new List<string>();
    public string equippedScissorId = "small_scissors";
}

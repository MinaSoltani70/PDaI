using System;

/// <summary>
/// Serializable snapshot of persistent gameplay data.
/// Add fields here as new systems join the save file.
/// </summary>
[Serializable]
public sealed class GameSaveData
{
    public const int CurrentVersion = 1;

    public int saveVersion = CurrentVersion;
    public int coins;
    public int hairUnits;
}

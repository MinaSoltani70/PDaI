using System;

[Serializable]
public sealed class GameSaveData
{
    public const int CurrentVersion = 1;

    public int saveVersion = CurrentVersion;
    public int coins;
    public int hairUnits;
}
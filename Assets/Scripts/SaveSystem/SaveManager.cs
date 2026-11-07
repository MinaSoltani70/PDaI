using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Loads and saves the shared game data file. Gameplay systems own their rules
/// and update only their fields on CurrentSave before requesting a save.
/// </summary>
[DefaultExecutionOrder(-100)]
public sealed class SaveManager : MonoBehaviour
{
    private const string SaveFileName = "game_save.json";

    private static readonly Regex CoinsFieldPattern = new Regex(
        "\"coins\"\\s*:\\s*(-?\\d+)",
        RegexOptions.CultureInvariant);
    private static readonly Regex HairUnitsFieldPattern = new Regex(
        "\"hairUnits\"\\s*:\\s*(-?\\d+)",
        RegexOptions.CultureInvariant);
    private static readonly Regex VersionFieldPattern = new Regex(
        "\"saveVersion\"\\s*:\\s*(\\d+)",
        RegexOptions.CultureInvariant);

    private GameSaveData currentSave;
    private bool isInitialized;

    public GameSaveData CurrentSave
    {
        get
        {
            InitializeIfNeeded();
            return currentSave;
        }
    }

    private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    private void Awake()
    {
        InitializeIfNeeded();
    }

    /// <summary>
    /// Writes the current shared save data to the persistent JSON file.
    /// </summary>
    public void Save()
    {
        InitializeIfNeeded();

        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            currentSave.saveVersion = GameSaveData.CurrentVersion;

            string temporaryPath = SavePath + ".tmp";
            string json = JsonUtility.ToJson(currentSave, true);
            File.WriteAllText(temporaryPath, json);

            if (File.Exists(SavePath))
            {
                File.Replace(temporaryPath, SavePath, null);
            }
            else
            {
                File.Move(temporaryPath, SavePath);
            }
        }
        catch (Exception exception) when (exception is IOException ||
                                          exception is UnauthorizedAccessException ||
                                          exception is ArgumentException ||
                                          exception is NotSupportedException)
        {
            Debug.LogWarning($"Could not save game data. Details: {exception.Message}", this);
        }
    }

    private void InitializeIfNeeded()
    {
        if (isInitialized)
        {
            return;
        }

        isInitialized = true;
        currentSave = new GameSaveData();

        if (File.Exists(SavePath))
        {
            TryLoad(SavePath, out currentSave);
        }
    }

    private bool TryLoad(string path, out GameSaveData data)
    {
        try
        {
            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json) ||
                !json.TrimStart().StartsWith("{", StringComparison.Ordinal) ||
                !json.TrimEnd().EndsWith("}", StringComparison.Ordinal))
            {
                throw new InvalidDataException("The file does not contain a JSON object.");
            }

            Match coinsMatch = CoinsFieldPattern.Match(json);
            if (!coinsMatch.Success ||
                !int.TryParse(coinsMatch.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedCoins) ||
                parsedCoins < 0)
            {
                throw new InvalidDataException("The JSON save does not contain a valid coin balance.");
            }

            data = JsonUtility.FromJson<GameSaveData>(json);
            if (data == null || data.coins != parsedCoins)
            {
                throw new InvalidDataException("The game save data is invalid.");
            }

            Match hairUnitsMatch = HairUnitsFieldPattern.Match(json);
            if (!hairUnitsMatch.Success)
            {
                data.hairUnits = 0;
            }
            else if (!int.TryParse(hairUnitsMatch.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedHairUnits) ||
                     parsedHairUnits < 0 || data.hairUnits != parsedHairUnits)
            {
                throw new InvalidDataException("The JSON save does not contain a valid hair inventory.");
            }

            Match versionMatch = VersionFieldPattern.Match(json);
            if (!versionMatch.Success)
            {
                data.saveVersion = GameSaveData.CurrentVersion;
            }
            else if (!int.TryParse(versionMatch.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int parsedVersion) ||
                     parsedVersion < 1 || parsedVersion != data.saveVersion)
            {
                throw new InvalidDataException("The game save version is invalid.");
            }

            return true;
        }
        catch (Exception exception) when (exception is IOException ||
                                          exception is UnauthorizedAccessException ||
                                          exception is ArgumentException ||
                                          exception is InvalidDataException)
        {
            data = new GameSaveData();
            Debug.LogWarning($"Could not load game data from '{path}'. Starting with a new save. Details: {exception.Message}", this);
            return false;
        }
    }
}

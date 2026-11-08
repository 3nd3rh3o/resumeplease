using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public sealed class GameOptionsData
{
    public int version = 1;
    public int width;
    public int height;
    public int refreshRate;
    public FullScreenMode mode;
    public bool vSync;
    public string bindings;
}

public sealed class GameOptions
{
    private readonly string path;
    private readonly InputActionAsset actions;
    public GameOptionsData Current { get; private set; }
    public string LastError { get; private set; }

    public GameOptions(string path, InputActionAsset actions)
    {
        this.path = path;
        this.actions = actions;
        Current = Defaults();
        if (File.Exists(path))
            try
            {
                var data = JsonUtility.FromJson<GameOptionsData>(File.ReadAllText(path));
                if (data == null || data.version != 1 || data.width < 640 || data.height < 360 ||
                    data.width > 16384 || data.height > 16384 || !Enum.IsDefined(typeof(FullScreenMode), data.mode))
                    throw new ArgumentException("Paramètres invalides.");
                Current = data;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
            { LastError = "Les options précédentes n'ont pas pu être lues ; les valeurs par défaut sont utilisées."; }
        try { RestoreBindings(Current.bindings); }
        catch (ArgumentException) { RestoreBindings(null); Current.bindings = null; LastError = "La configuration des touches était incompatible et a été réinitialisée."; }
        ApplyDisplay(Current);
    }

    private static GameOptionsData Defaults() => new GameOptionsData
    {
        width = Mathf.Max(640, Screen.width), height = Mathf.Max(360, Screen.height),
        refreshRate = Mathf.Max(1, Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value)),
        mode = Screen.fullScreenMode, vSync = QualitySettings.vSyncCount > 0
    };

    public void RestoreBindings(string json)
    {
        actions.RemoveAllBindingOverrides();
        if (!string.IsNullOrWhiteSpace(json)) actions.LoadBindingOverridesFromJson(json);
    }

    public bool Save(GameOptionsData data, out string error)
    {
        try
        {
            if (data.width < 640 || data.height < 360 || data.width > 16384 || data.height > 16384 || !Enum.IsDefined(typeof(FullScreenMode), data.mode))
                throw new ArgumentException("Paramètres d'affichage invalides.");
            data.bindings = actions.SaveBindingOverridesAsJson();
            SaveRepository.AtomicWrite(path, JsonUtility.ToJson(data, true));
            Current = data;
            LastError = null;
            ApplyDisplay(data);
            error = null;
            return true;
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
        {
            error = "Impossible d'enregistrer les options : " + exception.Message;
            return false;
        }
    }

    private static void ApplyDisplay(GameOptionsData data)
    {
        QualitySettings.vSyncCount = data.vSync ? 1 : 0;
        Screen.SetResolution(data.width, data.height, data.mode,
            new RefreshRate { numerator = (uint)Mathf.Max(1, data.refreshRate), denominator = 1 });
    }
}

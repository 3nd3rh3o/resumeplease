using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameSession : MonoBehaviour
{
    private static GameSession instance;
    public static GameSession Instance
    {
        get
        {
            if (instance == null) new GameObject("Game Session").AddComponent<GameSession>();
            return instance;
        }
    }
    public static bool InputBlocked => instance != null && (instance.IsLoading || (instance.Menus != null && instance.Menus.IsOpen));
    public static bool IsChangingScene => instance != null && instance.IsLoading;
    public GameContentCatalog Catalog { get; private set; }
    public SaveRepository Saves { get; private set; }
    public GameOptions Options { get; private set; }
    public GameMenuUI Menus { get; private set; }
    public bool IsLoading { get; private set; }
    private SaveGameData pendingLoad;
    public string LastSaveError { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance() => instance = null;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap() { var session = Instance; }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        Catalog = Resources.Load<GameContentCatalog>("GameContentCatalog");
        if (Catalog == null)
        {
            Debug.LogError("Catalogue du jeu absent. Utiliser Resume Please > Actualiser le catalogue dans l'éditeur.");
            enabled = false;
            return;
        }
        Saves = new SaveRepository(Path.Combine(Application.persistentDataPath, "Saves"));
        Options = new GameOptions(Path.Combine(Application.persistentDataPath, "options.json"), Catalog.inputActions);
        Menus = gameObject.AddComponent<GameMenuUI>();
        Menus.Initialize(this);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this) { SceneManager.sceneLoaded -= OnSceneLoaded; instance = null; }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.path == Catalog.mainMenuScene)
        {
            pendingLoad = null;
            IsLoading = false;
            Menus.ShowMain();
            return;
        }
        if (pendingLoad != null) InterviewResults.Instance.Restore(pendingLoad.campaign, Catalog);
    }

    public DaySnapshot TakeSnapshot(DayDefinition day)
    {
        if (pendingLoad == null) return null;
        if (pendingLoad.dayId != Catalog.Id(day)) throw new InvalidOperationException("La sauvegarde ne correspond pas à la journée chargée.");
        var state = pendingLoad.day;
        pendingLoad = null;
        return state;
    }

    public void NotifyReady()
    {
        IsLoading = false;
        Menus.HideLoading();
    }

    public void NewGame()
    {
        if (!CanLoadScene(Catalog.firstDayScene)) return;
        pendingLoad = null;
        InterviewResults.Instance.ClearResults();
        StartCoroutine(Navigate(Catalog.firstDayScene, true));
    }

    public List<SaveSlot> GetSlots()
    {
        var slots = Saves.List();
        foreach (var slot in slots)
            if (slot.data != null)
                try { SaveGameValidator.Validate(slot.data, Catalog); }
                catch (InvalidOperationException error) { slot.error = error.Message; slot.data = null; }
        return slots;
    }

    public SaveSlot Latest()
    {
        foreach (var slot in GetSlots()) if (slot.IsValid) return slot;
        return null;
    }

    public bool Load(string slotId)
    {
        if (IsLoading) return false;
        var slot = Saves.Read(slotId);
        if (!slot.IsValid) { Menus.ShowError("Chargement impossible : " + slot.error); return false; }
        try { SaveGameValidator.Validate(slot.data, Catalog); }
        catch (InvalidOperationException error) { Menus.ShowError("Chargement impossible : " + error.Message); return false; }
        pendingLoad = slot.data;
        StartCoroutine(Navigate(slot.data.scenePath, true));
        return true;
    }

    public bool SaveManual(string name)
    {
        var scenario = FindAnyObjectByType<ScenarioController>();
        if (scenario == null || !scenario.CanSave) { Menus.ShowError("Attendez la fin de la transition avant de sauvegarder."); return false; }
        return Write(scenario, Guid.NewGuid().ToString("N"), name);
    }

    public void SaveAutomatic(ScenarioController scenario)
    {
        if (Saves == null || scenario == null || !scenario.CanSave) return;
        Write(scenario, SaveRepository.AutoSlot, "Sauvegarde automatique");
    }

    private bool Write(ScenarioController scenario, string slot, string name)
    {
        try
        {
            var data = scenario.CaptureSave();
            SaveGameValidator.Validate(data, Catalog);
            if (Saves.Write(data, slot, name, out var error)) { LastSaveError = null; return true; }
            LastSaveError = error;
        }
        catch (InvalidOperationException error) { LastSaveError = error.Message; }
        Menus.ShowError(LastSaveError);
        return false;
    }

    public void MainMenu()
    {
        if (!CanLoadScene(Catalog.mainMenuScene)) return;
        SaveAutomatic(FindAnyObjectByType<ScenarioController>());
        pendingLoad = null;
        StartCoroutine(Navigate(Catalog.mainMenuScene, false));
    }

    private bool CanLoadScene(string path)
    {
        if (Application.CanStreamedLevelBeLoaded(path)) return true;
        Menus.ShowError("Scène absente des scènes du build : " + path);
        return false;
    }

    private IEnumerator Navigate(string scene, bool gameplay)
    {
        IsLoading = true;
        Menus.PrepareNavigation();
        if (gameplay) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        var operation = SceneManager.LoadSceneAsync(scene);
        while (!operation.isDone) yield return null;
    }

    public void Quit()
    {
        SaveAutomatic(FindAnyObjectByType<ScenarioController>());
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Kept explicit so automated tests never write to the player's save directory.
    public void SetStorageDirectoryForTests(string path)
    {
        Saves = new SaveRepository(Path.Combine(path, "Saves"));
        Options = new GameOptions(Path.Combine(path, "options.json"), Catalog.inputActions);
    }
}

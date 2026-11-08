using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public static class GameBuildTools
{
    public const string CatalogPath = "Assets/Resources/GameContentCatalog.asset";

    [MenuItem("Resume Please/Actualiser le catalogue et les scènes du build")]
    public static void RebuildCatalog()
    {
        const string inputPath = "Assets/InputSystem_Actions.inputactions";
        var parsed = InputActionAsset.FromJson(File.ReadAllText(inputPath));
        var player = parsed.FindActionMap("Player");
        if (player.FindAction("Pause", false) == null)
        {
            var pause = player.AddAction("Pause", InputActionType.Button);
            pause.AddBinding("<Keyboard>/escape", groups: "Keyboard&Mouse");
            File.WriteAllText(inputPath, parsed.ToJson());
        }
        Object.DestroyImmediate(parsed);
        AssetDatabase.ImportAsset(inputPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        var catalog = AssetDatabase.LoadAssetAtPath<GameContentCatalog>(CatalogPath);
        if (catalog == null) { catalog = ScriptableObject.CreateInstance<GameContentCatalog>(); AssetDatabase.CreateAsset(catalog, CatalogPath); }
        var previousScenes = new Dictionary<DayDefinition, string>();
        foreach (var entry in catalog.days) if (entry.asset != null) previousScenes[entry.asset] = entry.scenePath;
        catalog.inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(inputPath);
        catalog.candidates.Clear(); catalog.days.Clear(); catalog.documents.Clear();
        foreach (var id in AssetDatabase.FindAssets("t:Candidate", new[] { "Assets" }))
            catalog.candidates.Add(new GameContentCatalog.CandidateEntry { id = id, asset = AssetDatabase.LoadAssetAtPath<Candidate>(AssetDatabase.GUIDToAssetPath(id)) });
        foreach (var id in AssetDatabase.FindAssets("t:DayDefinition", new[] { "Assets" }))
        {
            var day = AssetDatabase.LoadAssetAtPath<DayDefinition>(AssetDatabase.GUIDToAssetPath(id));
            string scene = previousScenes.TryGetValue(day, out var existing) ? existing : "Assets/Scenes/" + day.name + ".unity";
            catalog.days.Add(new GameContentCatalog.DayEntry { id = id, asset = day, scenePath = scene });
        }
        foreach (var id in AssetDatabase.FindAssets("t:VisualTreeAsset", new[] { "Assets" }))
            catalog.documents.Add(new GameContentCatalog.DocumentEntry { id = id, asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(id)) });
        catalog.days.Sort((a, b) => string.CompareOrdinal(a.scenePath, b.scenePath));
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        var scenes = new List<EditorBuildSettingsScene>();
        if (File.Exists(catalog.mainMenuScene)) scenes.Add(new EditorBuildSettingsScene(catalog.mainMenuScene, true));
        foreach (var day in catalog.days) if (File.Exists(day.scenePath)) scenes.Add(new EditorBuildSettingsScene(day.scenePath, true));
        foreach (var old in EditorBuildSettings.scenes)
            if (!scenes.Exists(scene => scene.path == old.path)) scenes.Add(old);
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}

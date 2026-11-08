using UnityEditor;
using UnityEngine;

public static class DayContentTools
{
    public const string BasePrefabPath = "Assets/Prefabs/BaseScenePrefab.prefab";

    [MenuItem("Assets/Resume Please/Create day prefab variant", true)]
    private static bool CanCreateVariant() => Selection.activeObject is DayDefinition;

    [MenuItem("Assets/Resume Please/Create day prefab variant")]
    private static void CreateSelectedVariant()
    {
        var day = (DayDefinition)Selection.activeObject;
        var path = EditorUtility.SaveFilePanelInProject("Day prefab variant", day.name + "Scene", "prefab",
            "La variante héritera de BaseScenePrefab et référencera cette journée.", "Assets/Prefabs");
        if (string.IsNullOrEmpty(path)) return;
        Selection.activeObject = CreateVariant(day, path);
    }

    public static GameObject CreateVariant(DayDefinition day, string path)
    {
        var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefabPath);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
        try
        {
            var scenario = instance.GetComponent<ScenarioController>();
            var serialized = new SerializedObject(scenario);
            serialized.FindProperty("dayDefinition").objectReferenceValue = day;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(scenario);
            return PrefabUtility.SaveAsPrefabAsset(instance, path);
        }
        finally { Object.DestroyImmediate(instance); }
    }

    public static void InstallSharedView()
    {
        var root = PrefabUtility.LoadPrefabContents(BasePrefabPath);
        try
        {
            var hud = root.GetComponentInChildren<DayObjectivesHUD>(true);
            if (hud == null) hud = DayObjectivesHUD.CreateView(root.transform);
            var scenario = new SerializedObject(root.GetComponent<ScenarioController>());
            scenario.FindProperty("objectivesHUD").objectReferenceValue = hud;
            var briefing = root.GetComponentInChildren<DayBriefingUI>(true);
            if (briefing == null) briefing = DayBriefingUI.CreateView(root.transform);
            scenario.FindProperty("briefingUI").objectReferenceValue = briefing;
            var player = root.GetComponentInChildren<PlayerController>(true);
            AssignIfEmpty(scenario, "playerController", player);
            AssignIfEmpty(scenario, "playerRayCast", root.GetComponentInChildren<PlayerRayCast>(true));
            AssignIfEmpty(scenario, "playerBody", player == null ? null : player.transform);
            AssignIfEmpty(scenario, "playerHead", player == null ? null : player.transform.Find("Head"));
            AssignIfEmpty(scenario, "chair", root.GetComponentInChildren<Chair>(true));
            AssignIfEmpty(scenario, "exitDoor", root.GetComponentInChildren<DayExitDoor>(true));
            scenario.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, BasePrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void AssignIfEmpty(SerializedObject target, string name, Object value)
    {
        var property = target.FindProperty(name);
        if (property.objectReferenceValue == null) property.objectReferenceValue = value;
    }
}

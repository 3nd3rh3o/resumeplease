using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// Generated from asset GUIDs by GameBuildTools. References survive asset renames.
public sealed class GameContentCatalog : ScriptableObject
{
    [Serializable] public class CandidateEntry { public string id; public Candidate asset; }
    [Serializable] public class DayEntry { public string id; public DayDefinition asset; public string scenePath; }
    [Serializable] public class DocumentEntry { public string id; public VisualTreeAsset asset; }
    public string mainMenuScene = "Assets/Scenes/MainMenu.unity";
    public string firstDayScene = "Assets/Scenes/Day0.unity";
    public InputActionAsset inputActions;
    public List<CandidateEntry> candidates = new List<CandidateEntry>();
    public List<DayEntry> days = new List<DayEntry>();
    public List<DocumentEntry> documents = new List<DocumentEntry>();

    public string Id(Candidate asset)
    {
        if (asset == null) return "";
        foreach (var entry in candidates) if (entry.asset == asset) return entry.id;
        throw new InvalidOperationException("Candidat absent du catalogue : " + asset.name);
    }
    public string Id(DayDefinition asset)
    {
        foreach (var entry in days) if (entry.asset == asset) return entry.id;
        throw new InvalidOperationException("Journée absente du catalogue.");
    }
    public string Id(VisualTreeAsset asset)
    {
        if (asset == null) return "";
        foreach (var entry in documents) if (entry.asset == asset) return entry.id;
        throw new InvalidOperationException("Document absent du catalogue : " + asset.name);
    }
    public Candidate Candidate(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (var entry in candidates) if (entry.id == id && entry.asset != null) return entry.asset;
        throw new InvalidOperationException("Un candidat de la sauvegarde n'existe plus dans le contenu.");
    }
    public DayEntry Day(string id)
    {
        foreach (var entry in days) if (entry.id == id && entry.asset != null) return entry;
        throw new InvalidOperationException("La journée de cette sauvegarde n'existe plus.");
    }
    public VisualTreeAsset Document(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (var entry in documents) if (entry.id == id && entry.asset != null) return entry.asset;
        throw new InvalidOperationException("Un document de cette sauvegarde n'existe plus.");
    }
    public List<Candidate> Resolve(IEnumerable<string> ids)
    {
        var result = new List<Candidate>();
        foreach (var id in ids) result.Add(Candidate(id));
        return result;
    }
    public List<string> Identify(IEnumerable<Candidate> assets)
    {
        var result = new List<string>();
        foreach (var asset in assets) result.Add(Id(asset));
        return result;
    }
}

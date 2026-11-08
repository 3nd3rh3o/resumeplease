using System;
using System.Collections.Generic;
using UnityEngine;

public enum SavedDayPhase { Briefing, Review, Interview, Completed }
public enum SavedFocus { None, CV, Letter, Computer }

[Serializable]
public sealed class SavedDecision
{
    public string candidateId;
    public bool retained;
}

[Serializable]
public sealed class DaySnapshot
{
    public SavedDayPhase phase;
    public List<string> remaining = new List<string>();
    public List<string> selected = new List<string>();
    public string currentReview;
    public List<string> employeesAtStart = new List<string>();
    public List<SavedDecision> decisions = new List<SavedDecision>();
    public int interviewIndex;
    public int dialogueStep;
    public int question = -1;
    public Vector3 playerPosition;
    public Quaternion playerRotation = Quaternion.identity;
    public Quaternion headRotation = Quaternion.identity;
    public bool seated;
    public SavedFocus focus;
    public Vector2 cvScroll;
    public Vector2 letterScroll;
    public string computerDocument;
    public Quaternion originalHead = Quaternion.identity;
    public Quaternion originalBody = Quaternion.identity;
    public bool savedHead;
    public bool savedBody;
    public DayReport completedReport;
}

[Serializable]
public sealed class CampaignSnapshot
{
    public List<string> retained = new List<string>();
    public List<string> rejected = new List<string>();
    public List<DayReport> reports = new List<DayReport>();
}

[Serializable]
public sealed class SaveGameData
{
    public string magic = "ResumePlease";
    public int version = 1;
    public string slotId;
    public string name;
    public string savedAtUtc;
    public string dayTitle;
    public string dayId;
    public string scenePath;
    public DaySnapshot day;
    public CampaignSnapshot campaign;
}

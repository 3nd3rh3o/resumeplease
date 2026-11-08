using UnityEngine;

[CreateAssetMenu(fileName = "Skill", menuName = "Resume Please/Skill")]
public class SkillDefinition : ScriptableObject
{
    public string displayName;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
}

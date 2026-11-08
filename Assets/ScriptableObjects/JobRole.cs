using UnityEngine;

[CreateAssetMenu(fileName = "JobRole", menuName = "Resume Please/Job Role")]
public class JobRole : ScriptableObject
{
    public string displayName;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
}

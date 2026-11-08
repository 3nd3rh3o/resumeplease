using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using Unity.Properties;



[CreateAssetMenu(fileName = "MotivationLetters", menuName = "Scriptable Objects/MotivationLetters")]
public class MotivationLetters : ScriptableObject
{
    public string m_content;
    [CreateProperty] public List<VisualTreeAsset> uIDocuments;
}

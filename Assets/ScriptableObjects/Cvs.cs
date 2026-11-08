using UnityEngine;
using Unity.Properties;
using UnityEngine.UIElements;
using System.Collections.Generic;

[GeneratePropertyBag]
[CreateAssetMenu(fileName = "Cvs", menuName = "Scriptable Objects/Cvs")]
public class Cvs : ScriptableObject
{
    [CreateProperty] public string m_name;
    [CreateProperty] public string m_surname;
    [CreateProperty] public string m_email;
    [CreateProperty] public string m_phone;
    [CreateProperty] public string m_address;
    [CreateProperty] public string m_education;
    [CreateProperty] public string m_experience; 
    [CreateProperty] public string m_skills;
    [CreateProperty] public string m_hobbies;
    [CreateProperty] public string m_languages;
    [CreateProperty] public string m_references;
    [CreateProperty] public List<VisualTreeAsset> uIDocuments;
}

using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InterviewDialogue", menuName = "Scriptable Objects/Interview Dialogue")]
public class InterviewDialogue : ScriptableObject
{
    [Serializable]
    public class Question
    {
        [TextArea(2, 5)] public string question;
        [TextArea(3, 10)] public string response;
    }

    [Serializable]
    public class Step
    {
        public Question first = new Question();
        public Question second = new Question();
    }

    public List<Step> steps = new List<Step>();
}

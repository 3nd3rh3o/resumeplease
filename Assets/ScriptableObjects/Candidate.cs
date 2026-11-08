using UnityEngine;

[CreateAssetMenu(fileName = "Candidate", menuName = "Scriptable Objects/Candidate")]
public class Candidate : ScriptableObject
{
    public Cvs m_CV;
    public MotivationLetters m_motivationLetter;

    public GameObject mesh;
    public InterviewDialogue interviewDialogue;
    public CandidateProfile profile = new CandidateProfile();
    
}

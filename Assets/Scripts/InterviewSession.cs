using System.Collections.Generic;

// Runtime progress belongs to the interview, never to the shared dialogue asset.
public sealed class InterviewSession
{
    private readonly IReadOnlyList<InterviewDialogue.Step> steps;
    public int StepIndex { get; private set; }
    public int StepCount => steps.Count;
    public int SelectedQuestion { get; private set; } = -1;
    public bool AwaitingDecision => StepIndex >= steps.Count;
    public bool IsComplete { get; private set; }
    public bool Retained { get; private set; }
    public event System.Action Changed;
    public InterviewDialogue.Step CurrentStep => AwaitingDecision ? null : steps[StepIndex];

    public InterviewSession(InterviewDialogue dialogue)
    {
        steps = dialogue != null && dialogue.steps != null
            ? new List<InterviewDialogue.Step>(dialogue.steps)
            : new List<InterviewDialogue.Step>();
    }

    public bool ChooseQuestion(int index)
    {
        if (IsComplete || AwaitingDecision || SelectedQuestion != -1 || (index != 0 && index != 1))
            return false;
        SelectedQuestion = index;
        Changed?.Invoke();
        return true;
    }

    public string Response => SelectedQuestion < 0 || CurrentStep == null ? string.Empty
        : (SelectedQuestion == 0 ? CurrentStep.first : CurrentStep.second)?.response ?? string.Empty;

    public bool Continue()
    {
        if (IsComplete || AwaitingDecision || SelectedQuestion < 0)
            return false;
        StepIndex++;
        SelectedQuestion = -1;
        Changed?.Invoke();
        return true;
    }

    public bool Decide(bool retained)
    {
        if (IsComplete || !AwaitingDecision)
            return false;
        Retained = retained;
        IsComplete = true;
        Changed?.Invoke();
        return true;
    }

    public static InterviewSession Restore(InterviewDialogue dialogue, int step, int question)
    {
        var session = new InterviewSession(dialogue);
        if (step < 0 || step > session.StepCount || question < -1 || question > 1 || (step == session.StepCount && question != -1))
            throw new System.InvalidOperationException("Étape de dialogue invalide ou contenu modifié depuis la sauvegarde.");
        session.StepIndex = step;
        session.SelectedQuestion = question;
        return session;
    }
}

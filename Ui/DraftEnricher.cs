using SupportAgent.Agent;
using SupportAgent.Tools;

namespace SupportAgent.Ui;

public static class DraftEnricher
{
    public static StagedDraft Enrich(StagedDraft draft, TicketTrace? trace)
    {
        if (trace is null)
        {
            return draft;
        }

        return draft with
        {
            EvidenceConfidence = draft.EvidenceConfidence ?? trace.EvidenceConfidence,
            AnswerConfidence = draft.AnswerConfidence ?? trace.AnswerConfidence,
            CheckerPass = draft.CheckerPass ?? trace.CheckerPass,
            CheckerProblems = draft.CheckerProblems is { Count: > 0 }
                ? draft.CheckerProblems
                : trace.CheckerProblems,
            SuggestedAction = draft.SuggestedAction ?? trace.SuggestedAction,
        };
    }
}

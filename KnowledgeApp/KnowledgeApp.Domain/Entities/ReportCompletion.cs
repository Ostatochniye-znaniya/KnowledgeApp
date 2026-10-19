namespace KnowledgeApp.Domain.Entities;

public static class ReportCompletion
{
    public static bool? Calculate(bool? doneInPaperForm, bool? doneInElectronicForm)
    {
        if (doneInPaperForm == false || doneInElectronicForm == false)
            return false;

        return doneInPaperForm == true && doneInElectronicForm == true ? true : null;
    }
}

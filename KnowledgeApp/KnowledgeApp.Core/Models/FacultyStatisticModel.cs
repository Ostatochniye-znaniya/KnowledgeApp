namespace KnowledgeApp.Core.Models;

public class FacultyStatisticModel
{
    public FacultyStatisticModel(string facultyName, int? groupcountmust, int? groupcountfact, int? disciplinecountmust, int? disciplinecountfact, int? ereportdonecount, int? ereportrevcount, int? papreportdeonecount, int? papreportrevcount,  bool? alldone)
    {
        FacultyName = facultyName;
        GroupCountMust = groupcountmust;
        GroupCountFact = groupcountfact;
        DisciplineCountMust = disciplinecountmust;
        DisciplineCountFact = disciplinecountfact;
        EReportDoneCount = ereportdonecount;
        EReportRevCount = ereportrevcount;
        PapReportDoneCount = papreportdeonecount;
        PapReportRevCount = papreportrevcount;
        AllDone = alldone;
    }
    public FacultyStatisticModel(int id, string facultyName, int? groupcountmust, int? groupcountfact, int? disciplinecountmust, int? disciplinecountfact, int? ereportdonecount, int? ereportrevcount, int? papreportdeonecount, int? papreportrevcount, bool? alldone)
    {
        Id = id;
        FacultyName = facultyName;
        GroupCountMust = groupcountmust;
        GroupCountFact = groupcountfact;
        DisciplineCountMust = disciplinecountmust;
        DisciplineCountFact = disciplinecountfact;
        EReportDoneCount = ereportdonecount;
        EReportRevCount = ereportrevcount;
        PapReportDoneCount = papreportdeonecount;
        PapReportRevCount = papreportrevcount;
        AllDone = alldone;
    }

    public int Id { get; set; }

    public string FacultyName { get; set; } = null!;
    public int? GroupCountMust { get; set; }
    public int? GroupCountFact { get; set; }
    public int? DisciplineCountMust { get; set; }
    public int? DisciplineCountFact { get; set; }
    public int? EReportDoneCount { get; set; }
    public int? EReportRevCount { get; set; }
    public int? PapReportDoneCount { get; set; }
    public int? PapReportRevCount { get; set; }
    public bool? AllDone { get; set; }

}


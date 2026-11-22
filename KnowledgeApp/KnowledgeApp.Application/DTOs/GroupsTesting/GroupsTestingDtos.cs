using System.Text.Json.Serialization;

namespace KnowledgeApp.Application.DTOs.GroupsTesting;

public sealed record GroupsTestingResponse(
    int CurrentSemesterId, long Version, bool RequiresRecalculation,
    IReadOnlyList<TestingSemesterDto> Semesters, IReadOnlyList<TestingStudyProgramDto> StudyPrograms,
    StudentListFormationPeriodDto? StudentListFormationPeriod = null);
public sealed record StudentListFormationPeriodDto(
    [property: JsonRequired] DateOnly StartDate, [property: JsonRequired] DateOnly EndDate);
public sealed record TestingSemesterDto(int Id, int SemesterYear, int SemesterPart, string Period);
public sealed record TestingStudyProgramDto(
    int StudyProgramId, string StudyProgramName, string? CypherOfTheDirection,
    int? FacultyId, string? FacultyName, IReadOnlyList<TestingGroupDto> Groups,
    ParticipationDecisionDto? ParticipationDecision = null);
public sealed record TestingGroupDto(
    int GroupId, string GroupNumber, int? Course,
    IReadOnlyList<TestingHistoryCellDto> TestingHistory, IReadOnlyList<PlannedTestingCellDto> PlannedTestings,
    bool IsActive = true);
public sealed record TestingHistoryCellDto(int SemesterId, bool? Completed);
public sealed record PlannedTestingCellDto(
    int SemesterId, bool Planned, string? Author, bool Editable, string? RestrictionReason);
public sealed class UpdateGroupsTestingRequest
{
    public int CurrentSemesterId { get; set; }
    public long? Version { get; set; }
    public List<GroupsTestingChange>? Changes { get; set; }
    public StudentListFormationPeriodDto? StudentListFormationPeriod { get; set; }
}
public sealed record GroupsTestingChange(int GroupId, int SemesterId, [property: JsonRequired] bool Planned);

public sealed record ParticipationDecisionDto(int? RecommendedGroupId, int? SelectedGroupId,
    string? Reason, DateTime DecidedAt);
public sealed record HodGroupsResponse(int DepartmentId, string DepartmentName,
    GroupsTestingResponse? Plan, IReadOnlyList<HodProfileChoiceDto> Choices);
public sealed record HodProfileChoiceDto(int StudyProgramId, int? RecommendedGroupId, int? SelectedGroupId,
    bool Reviewed, string? Reason, bool Editable, string? RestrictionReason, IReadOnlyList<int> EligibleGroupIds);
public sealed class UpdateHodGroupsRequest
{
    public int CurrentSemesterId { get; set; }
    public long? Version { get; set; }
    public List<HodProfileDecisionRequest>? Decisions { get; set; }
}
public sealed record HodProfileDecisionRequest(int StudyProgramId,
    [property: JsonRequired] int? SelectedGroupId, string? Reason);

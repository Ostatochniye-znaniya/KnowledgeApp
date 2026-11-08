using System.Text.Json.Serialization;

namespace KnowledgeApp.Application.DTOs.GroupsTesting;

public sealed record GroupsTestingResponse(
    int CurrentSemesterId, long Version, bool RequiresRecalculation,
    IReadOnlyList<TestingSemesterDto> Semesters, IReadOnlyList<TestingStudyProgramDto> StudyPrograms);
public sealed record TestingSemesterDto(int Id, int SemesterYear, int SemesterPart, string Period);
public sealed record TestingStudyProgramDto(
    int StudyProgramId, string StudyProgramName, string? CypherOfTheDirection,
    int? FacultyId, string? FacultyName, IReadOnlyList<TestingGroupDto> Groups);
public sealed record TestingGroupDto(
    int GroupId, string GroupNumber, int? Course,
    IReadOnlyList<TestingHistoryCellDto> TestingHistory, IReadOnlyList<PlannedTestingCellDto> PlannedTestings);
public sealed record TestingHistoryCellDto(int SemesterId, bool? Completed);
public sealed record PlannedTestingCellDto(
    int SemesterId, bool Planned, string? Author, bool Editable, string? RestrictionReason);
public sealed class UpdateGroupsTestingRequest
{
    public int CurrentSemesterId { get; set; }
    public long? Version { get; set; }
    public List<GroupsTestingChange>? Changes { get; set; }
}
public sealed record GroupsTestingChange(int GroupId, int SemesterId, [property: JsonRequired] bool Planned);

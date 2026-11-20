using KnowledgeApp.Application.Services;

namespace KnowledgeApp.UnitTests.Services;

/// <summary>
/// Правило назначающей кафедры (ТЗ, п. 2.5): своя дисциплина — назначает кафедра группы;
/// дисциплина другого факультета — кафедра дисциплины, но только после заявки от кафедры группы.
/// </summary>
public class TestingManagingDepartmentTests
{
    private const int ItFaculty = 1;
    private const int BasicFaculty = 2;

    private const int GroupDepartment = 4;
    private const int OtherItDepartment = 2;
    private const int BasicDepartment = 6;

    private static readonly DateTime RequestSentAt = new(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void SameDepartment_ManagedByGroupDepartment()
    {
        var info = Info(GroupDepartment, ItFaculty, GroupDepartment, ItFaculty, requestSentAt: null);

        Assert.False(info.IsExternal);
        Assert.Equal(GroupDepartment, info.ManagingDepartmentId);
    }

    [Fact]
    public void OtherDepartmentOfSameFaculty_StillManagedByGroupDepartment()
    {
        var info = Info(GroupDepartment, ItFaculty, OtherItDepartment, ItFaculty, requestSentAt: null);

        Assert.False(info.IsExternal);
        Assert.Equal(GroupDepartment, info.ManagingDepartmentId);
    }

    [Fact]
    public void SameFaculty_RequestIsIgnored()
    {
        // Заявка нужна только для межфакультетских дисциплин и ничего не меняет внутри факультета
        var info = Info(GroupDepartment, ItFaculty, OtherItDepartment, ItFaculty, RequestSentAt);

        Assert.Equal(GroupDepartment, info.ManagingDepartmentId);
    }

    [Fact]
    public void OtherFaculty_WithoutRequest_NobodyManagesYet()
    {
        var info = Info(GroupDepartment, ItFaculty, BasicDepartment, BasicFaculty, requestSentAt: null);

        Assert.True(info.IsExternal);
        Assert.Null(info.ManagingDepartmentId);
    }

    [Fact]
    public void OtherFaculty_AfterRequest_ManagedByDisciplineDepartment()
    {
        var info = Info(GroupDepartment, ItFaculty, BasicDepartment, BasicFaculty, RequestSentAt);

        Assert.True(info.IsExternal);
        Assert.Equal(BasicDepartment, info.ManagingDepartmentId);
    }

    [Theory]
    [InlineData(null, ItFaculty)]
    [InlineData(ItFaculty, null)]
    public void UnknownFacultyOnOneSide_TreatedAsExternal(int? groupFaculty, int? disciplineFaculty)
    {
        var info = Info(GroupDepartment, groupFaculty, BasicDepartment, disciplineFaculty, requestSentAt: null);

        Assert.True(info.IsExternal);
        Assert.Null(info.ManagingDepartmentId);
    }

    [Fact]
    public void BothFacultiesUnknown_ManagedByGroupDepartment()
    {
        var info = Info(GroupDepartment, null, BasicDepartment, null, requestSentAt: null);

        Assert.False(info.IsExternal);
        Assert.Equal(GroupDepartment, info.ManagingDepartmentId);
    }

    private static TestingManagingDepartment.Info Info(
        int? groupDepartment,
        int? groupFaculty,
        int? disciplineDepartment,
        int? disciplineFaculty,
        DateTime? requestSentAt) =>
        new(groupDepartment, groupFaculty, disciplineDepartment, disciplineFaculty, requestSentAt);
}

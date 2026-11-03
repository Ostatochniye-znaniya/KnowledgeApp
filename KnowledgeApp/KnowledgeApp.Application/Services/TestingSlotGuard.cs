using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Infrastructure.Context;
using KnowledgeApp.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Services;

public static class TestingSlotGuard
{
    public static async Task EnsureSlotIsAvailable(
        KnowledgeTestDbContext context,
        Testing currentTesting,
        DateOnly selectedDate,
        TimeOnly selectedStart,
        CancellationToken cancellationToken)
    {
        var assignedTeacherIds = await context.TestingTeacherApprovals
            .Where(a => a.TestingId == currentTesting.Id)
            .Select(a => a.TeacherId)
            .ToListAsync(cancellationToken);
        if (assignedTeacherIds.Count == 0)
            throw new DocumentAccessException("На тестирование не назначены преподаватели.");

        await EnsureLocationIsAvailable(
            context,
            currentTesting,
            selectedDate,
            selectedStart,
            currentTesting.IsInPerson,
            currentTesting.Room,
            currentTesting.LmsUrl,
            cancellationToken);

        var dayStart = selectedDate.ToDateTime(TimeOnly.MinValue);
        var dayEnd = dayStart.AddDays(1);
        var otherTestings = await context.Testings
            .Where(t => t.Id != currentTesting.Id &&
                        t.GroupId == currentTesting.GroupId &&
                        t.ScheduledDate >= dayStart &&
                        t.ScheduledDate < dayEnd &&
                        t.ScheduledTime.HasValue)
            .Select(t => new { t.Id, t.ScheduledTime })
            .ToListAsync(cancellationToken);
        if (otherTestings.Any(t =>
                TestingPairSchedule.Overlaps(selectedStart, TimeOnly.FromTimeSpan(t.ScheduledTime!.Value))))
        {
            throw new DocumentConflictException("У группы уже есть проверка в это время.");
        }

        var scheduledTestingIds = await context.Testings
            .Where(t => t.Id != currentTesting.Id &&
                        t.ScheduledDate >= dayStart &&
                        t.ScheduledDate < dayEnd &&
                        t.ScheduledTime.HasValue)
            .Select(t => new { t.Id, t.ScheduledTime })
            .ToListAsync(cancellationToken);
        var overlappingTestingIds = scheduledTestingIds
            .Where(t => TestingPairSchedule.Overlaps(
                selectedStart, TimeOnly.FromTimeSpan(t.ScheduledTime!.Value)))
            .Select(t => t.Id)
            .ToList();
        if (overlappingTestingIds.Count == 0)
            return;

        var hasTeacherConflict = await context.TestingTeacherApprovals
            .AnyAsync(a => overlappingTestingIds.Contains(a.TestingId) &&
                           assignedTeacherIds.Contains(a.TeacherId), cancellationToken);
        if (hasTeacherConflict)
            throw new DocumentConflictException("У одного из назначенных преподавателей уже есть проверка в это время.");
    }

    public static async Task EnsureLocationIsAvailable(
        KnowledgeTestDbContext context,
        Testing currentTesting,
        DateOnly selectedDate,
        TimeOnly selectedStart,
        bool? isInPerson,
        string? room,
        string? lmsUrl,
        CancellationToken cancellationToken)
    {
        if (isInPerson is null)
            return;

        var dayStart = selectedDate.ToDateTime(TimeOnly.MinValue);
        var dayEnd = dayStart.AddDays(1);
        var otherTestings = await context.Testings
            .Where(t => t.Id != currentTesting.Id &&
                        t.ScheduledDate >= dayStart &&
                        t.ScheduledDate < dayEnd &&
                        t.ScheduledTime.HasValue)
            .Select(t => new { t.ScheduledTime, t.IsInPerson, t.Room, t.LmsUrl })
            .ToListAsync(cancellationToken);

        foreach (var other in otherTestings)
        {
            var otherStart = TimeOnly.FromTimeSpan(other.ScheduledTime!.Value);
            if (!TestingPairSchedule.Overlaps(selectedStart, otherStart))
                continue;

            if (isInPerson.Value && other.IsInPerson == true &&
                !string.IsNullOrWhiteSpace(room) &&
                string.Equals(room.Trim(), other.Room?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                throw new DocumentConflictException(
                    $"Аудитория «{room.Trim()}» уже занята в это время.");
            }

            if (!isInPerson.Value && other.IsInPerson == false &&
                !string.IsNullOrWhiteSpace(lmsUrl) &&
                string.Equals(lmsUrl.Trim(), other.LmsUrl?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                throw new DocumentConflictException("Эта ссылка LMS уже используется в это время.");
            }
        }
    }
}

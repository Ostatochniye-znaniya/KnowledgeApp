using KnowledgeApp.Application.DTOs;

namespace KnowledgeApp.Application.Services;

public static class TestingPairSchedule
{
    private static readonly IReadOnlyList<TestingPairSlotDto> Slots =
    [
        new(1, new TimeOnly(9, 0), new TimeOnly(10, 30)),
        new(2, new TimeOnly(10, 40), new TimeOnly(12, 10)),
        new(3, new TimeOnly(12, 40), new TimeOnly(14, 10)),
        new(4, new TimeOnly(14, 20), new TimeOnly(15, 50)),
        new(5, new TimeOnly(16, 0), new TimeOnly(17, 30)),
        new(6, new TimeOnly(17, 40), new TimeOnly(19, 10)),
        new(7, new TimeOnly(19, 20), new TimeOnly(20, 50))
    ];

    public static IReadOnlyList<TestingPairSlotDto> All => Slots;

    public static TestingPairSlotDto Get(int pairNumber) =>
        Slots.FirstOrDefault(slot => slot.PairNumber == pairNumber)
        ?? throw new ArgumentOutOfRangeException(nameof(pairNumber), "Указан несуществующий номер пары.");

    public static bool Overlaps(TimeOnly firstStart, TimeOnly secondStart)
    {
        const long durationTicks = TimeSpan.TicksPerMinute * 90;
        return firstStart.Ticks < secondStart.Ticks + durationTicks &&
               secondStart.Ticks < firstStart.Ticks + durationTicks;
    }
}

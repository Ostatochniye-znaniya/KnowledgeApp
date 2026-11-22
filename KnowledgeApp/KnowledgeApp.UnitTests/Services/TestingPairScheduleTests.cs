using KnowledgeApp.Application.Services;

namespace KnowledgeApp.UnitTests.Services;

public class TestingPairScheduleTests
{
    [Fact]
    public void All_HasSevenPairsOfNinetyMinutesInOrder()
    {
        var slots = TestingPairSchedule.All;

        Assert.Equal(Enumerable.Range(1, 7), slots.Select(s => s.PairNumber));
        Assert.All(slots, s => Assert.Equal(TimeSpan.FromMinutes(90), s.EndsAt - s.StartsAt));
        for (var i = 1; i < slots.Count; i++)
        {
            Assert.True(slots[i - 1].EndsAt <= slots[i].StartsAt,
                $"Пара {slots[i].PairNumber} начинается раньше, чем заканчивается пара {slots[i - 1].PairNumber}");
        }
    }

    [Theory]
    [InlineData(1, 9, 0, 10, 30)]
    [InlineData(3, 12, 40, 14, 10)] // после большого перерыва
    [InlineData(7, 19, 20, 20, 50)]
    public void Get_ReturnsPairTimes(int pair, int startHour, int startMinute, int endHour, int endMinute)
    {
        var slot = TestingPairSchedule.Get(pair);

        Assert.Equal(new TimeOnly(startHour, startMinute), slot.StartsAt);
        Assert.Equal(new TimeOnly(endHour, endMinute), slot.EndsAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(-1)]
    public void Get_UnknownPair_Throws(int pair)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TestingPairSchedule.Get(pair));
    }

    [Theory]
    [InlineData(9, 0, 9, 0)]     // одно и то же время
    [InlineData(9, 0, 10, 0)]    // вторая начинается во время первой
    [InlineData(10, 0, 9, 0)]    // порядок аргументов не важен
    [InlineData(9, 0, 10, 29)]   // за минуту до конца
    public void Overlaps_WhenIntervalsIntersect_ReturnsTrue(int h1, int m1, int h2, int m2)
    {
        Assert.True(TestingPairSchedule.Overlaps(new TimeOnly(h1, m1), new TimeOnly(h2, m2)));
    }

    [Theory]
    [InlineData(9, 0, 10, 30)]   // вторая начинается ровно в момент окончания первой
    [InlineData(10, 30, 9, 0)]
    [InlineData(9, 0, 10, 40)]   // соседние пары по расписанию
    [InlineData(9, 0, 17, 40)]
    public void Overlaps_WhenIntervalsDoNotIntersect_ReturnsFalse(int h1, int m1, int h2, int m2)
    {
        Assert.False(TestingPairSchedule.Overlaps(new TimeOnly(h1, m1), new TimeOnly(h2, m2)));
    }

    [Fact]
    public void Overlaps_NoTwoScheduledPairsOverlap()
    {
        var slots = TestingPairSchedule.All;

        foreach (var a in slots)
        foreach (var b in slots.Where(s => s.PairNumber != a.PairNumber))
            Assert.False(TestingPairSchedule.Overlaps(a.StartsAt, b.StartsAt),
                $"Пары {a.PairNumber} и {b.PairNumber} пересекаются");
    }
}

using KnowledgeApp.Application.Services;

namespace KnowledgeApp.UnitTests.Services;

public class PersonNameTests
{
    private const char Nbsp = ' ';

    [Theory]
    [InlineData("Иванов Иван Иванович", "Иванову", "Иванова")]
    [InlineData("Петрова Анна Сергеевна", "Петровой", "Петрову")]
    [InlineData("Достоевский Фёдор Михайлович", "Достоевскому", "Достоевского")]
    [InlineData("Толстой Лев Николаевич", "Толстому", "Толстого")]
    [InlineData("Шевчук Юрий Юлианович", "Шевчуку", "Шевчука")]
    [InlineData("Гайдай Леонид Иович", "Гайдаю", "Гайдая")]
    [InlineData("Берия Лаврентий Павлович", "Берии", "Берию")]
    [InlineData("Римский-Корсаков Николай Андреевич", "Римскому-Корсакову", "Римского-Корсакова")]
    [InlineData("Ковальская Ольга Петровна", "Ковальской", "Ковальскую")]
    public void DeclinesSurnameAndShortensInitials(string fullName, string dativeSurname, string accusativeSurname)
    {
        var parts = fullName.Split(' ');
        var initials = $"{parts[1][0]}.{parts[2][0]}.{Nbsp}";

        Assert.Equal(initials + dativeSurname, PersonName.Dative(fullName));
        Assert.Equal(initials + accusativeSurname, PersonName.Accusative(fullName));
    }

    [Theory]
    [InlineData("Черных Ольга Петровна", "Черных")] // несклоняемые фамилии
    [InlineData("Шевчук Анна Сергеевна", "Шевчук")]
    [InlineData("Седых Пётр Ильич", "Седых")]
    public void KeepsIndeclinableSurnames(string fullName, string surname)
    {
        Assert.EndsWith(Nbsp + surname, PersonName.Dative(fullName));
        Assert.EndsWith(Nbsp + surname, PersonName.Accusative(fullName));
    }

    [Theory]
    [InlineData("Иванов Иван")] // нет отчества
    [InlineData("Иванов Иван Иванович Младший")]
    [InlineData("Ivanov2 Ivan Ivanovich")] // не только буквы
    [InlineData("")]
    public void ReturnsInputUnchanged_WhenNotFullName(string fullName)
    {
        Assert.Equal(fullName, PersonName.Dative(fullName));
        Assert.Equal(fullName, PersonName.Accusative(fullName));
    }

    [Fact]
    public void UnknownPatronymic_KeepsSurnameAsIs()
    {
        Assert.Equal($"Д.О.{Nbsp}Смит", PersonName.Dative("Смит Джон Оглы"));
    }
}

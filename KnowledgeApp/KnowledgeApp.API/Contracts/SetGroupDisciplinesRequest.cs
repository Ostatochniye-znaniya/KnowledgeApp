namespace KnowledgeApp.API.Contracts;

public class SetGroupDisciplinesRequest
{
    /// <summary>Выбранные дисциплины (не больше двух). Пустой список снимает выбор.</summary>
    public List<int> DisciplineIds { get; set; } = new();

    /// <summary>Семестр; если не передан — самый поздний.</summary>
    public int? SemesterId { get; set; }
}

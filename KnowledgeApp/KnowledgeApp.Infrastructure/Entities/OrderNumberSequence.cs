namespace KnowledgeApp.Infrastructure.Entities;

// Одна строка блокируется при выдаче официального номера. Черновики счётчик не расходуют.
public sealed class OrderNumberSequence
{
    public int Id { get; set; }
    public long LastNumber { get; set; }
}

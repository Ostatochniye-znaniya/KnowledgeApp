namespace KnowledgeApp.API.Contracts
{
    public class DisciplineResponse
    {
        // Ответ с данными дисциплины
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; } // Дополнительное поле для отображения
    }
}
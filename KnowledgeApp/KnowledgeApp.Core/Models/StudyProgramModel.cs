namespace KnowledgeApp.Core.Models
{
    public class StudyProgramModel
    {
        public StudyProgramModel() { }
        public StudyProgramModel(int id, string name, int? departmentId, string? cypherOfTheDirection)
        {
            Id = id;
            Name = name;
            DepartmentId = departmentId;
            CypherOfTheDirection = cypherOfTheDirection;
        }
        public StudyProgramModel(string name, int? departmentId, string? cypherOfTheDirection)
        {
            Name = name;
            DepartmentId = departmentId;
            CypherOfTheDirection = cypherOfTheDirection;
        }
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int? DepartmentId { get; set; }
        public string? CypherOfTheDirection { get; set; }
        public List<int> StudyGroupIds { get; set; } = new List<int>();
    }
}
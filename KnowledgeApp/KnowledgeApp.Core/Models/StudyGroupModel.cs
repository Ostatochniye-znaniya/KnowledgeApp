namespace KnowledgeApp.Core.Models
{
    public class StudyGroupModel
    {
        public StudyGroupModel() { }
        public StudyGroupModel(int id, string groupNumber, int studyProgramId)
        {
            Id = id;
            GroupNumber = groupNumber;
            StudyProgramId = studyProgramId;
        }
        public StudyGroupModel(string groupNumber, int studyProgramId)
        {
            GroupNumber = groupNumber;
            StudyProgramId = studyProgramId;
        }
        public int Id { get; set; }
        public string GroupNumber { get; set; } = null!;
        public int StudyProgramId { get; set; }
        public List<int> StudentIds { get; set; } = new List<int>();
        public List<int> TestingIds { get; set; } = new List<int>();
    }
}

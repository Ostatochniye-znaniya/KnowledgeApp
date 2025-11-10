using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeApp.Core.Models
{
    public class RecommendationHistoryModel
    {
        public int Id { get; set; }
        public DateTime RecommendedAt { get; set; }
        public string? RecommendedBy { get; set; }
        public int? RecommendedById { get; set; }
        public int SemesterId { get; set; }
        public int StudyGroupId { get; set; }
        public string? SemesterName { get; set; }
        public string? StudyGroupName { get; set; }
    }

}

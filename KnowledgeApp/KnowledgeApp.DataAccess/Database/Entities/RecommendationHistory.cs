using KnowledgeApp.DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeApp.DataAccess.Database.Entities
{
    public partial class RecommendationHistory
    {
        public int Id { get; set; }
        public DateTime RecommendedAt { get; set; }
        public int? UserId { get; set; }
        public User? User { get; set; }
        public int SemesterId { get; set; }
        public Semester? Semester { get; set; }
        public int StudyGroupId { get; set; }
        public StudyGroup? StudyGroup { get; set; }
    }
    
}

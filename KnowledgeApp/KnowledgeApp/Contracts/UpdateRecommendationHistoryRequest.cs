using System.ComponentModel.DataAnnotations;

namespace KnowledgeApp.API.Contracts
{
    public class UpdateRecommendationHistoryRequest
    {
        [Required]
        public DateTime RecommendedAt { get; set; }

        [Required]
        public string RecommendedBy { get; set; } = string.Empty;

        [Required]
        public int SemesterId { get; set; }

        [Required]
        public int StudyGroupId { get; set; }
    }
}

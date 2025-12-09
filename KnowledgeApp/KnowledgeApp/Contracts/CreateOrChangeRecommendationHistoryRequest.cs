using System.ComponentModel.DataAnnotations;

namespace KnowledgeApp.API.Contracts
{
    public class CreateOrChangeRecommendationHistoryRequest
    {
        public int? Id { get; set; } 
        [Required]
        public int RecommendedById { get; set; }

        [Required]
        public int SemesterId { get; set; }

        [Required]
        public int StudyGroupId { get; set; }
    }
}

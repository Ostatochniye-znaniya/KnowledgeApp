using System.ComponentModel.DataAnnotations;

namespace KnowledgeApp.API.Contracts;

public class CreateRecommendationHistoryRequest
{
    [RegularExpression("^(automatic|manual)$")]
    public string Author { get; set; } = "manual";
    [Required]
    public DateTime RecommendedAt { get; set; }

    [Required]
    public int RecommendedById { get; set; }

    [Required]
    public int SemesterId { get; set; }

    [Required]
    public int StudyGroupId { get; set; }
}

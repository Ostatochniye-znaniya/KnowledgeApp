using System.ComponentModel.DataAnnotations;

namespace KnowledgeApp.API.Contracts;

public class UpdateRecommendationHistoryRequest
{
    [RegularExpression("^(automatic|manual)$")]
    public string Author { get; set; } = "manual";
    public int? RecommendedById { get; set; }
    [Required]
    public DateTime RecommendedAt { get; set; }

    [Required]
    public string RecommendedBy { get; set; } = string.Empty;

    [Required]
    public int SemesterId { get; set; }

    [Required]
    public int StudyGroupId { get; set; }
}

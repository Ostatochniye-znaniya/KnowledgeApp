using System.ComponentModel.DataAnnotations;

namespace KnowledgeApp.API.Contracts
{
    public class GetSemesterTimelineRequest
    {
        public int FromYear { get; set; }
        [Range(1,2)]
        public int FromPart { get; set; }
        public int ToYear { get; set; }
        [Range(1,2)]
        public int ToPart { get; set; }
    }
}

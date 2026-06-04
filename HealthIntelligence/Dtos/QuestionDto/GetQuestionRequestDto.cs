using HealthIntelligence.Dtos.CommonDto;

namespace HealthIntelligence.Dtos.QuestionDto
{
    public class GetQuestionRequestDto : PaginationRequest
    {
        public int? PillarID { get; set; }
    }
}

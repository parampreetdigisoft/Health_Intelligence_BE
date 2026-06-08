using HealthIntelligence.Models;

namespace HealthIntelligence.Dtos.QuestionDto
{
    public class GetQuestionRespones : AddUpdateQuestionDto
    {
        public int DisplayOrder { get; set; }
        public string PillarName { get; set; }
    }
    public class GetQuestionByCountryRespones : GetQuestionRespones
    {
        public int AssessmentID { get; set; }
        public int PillarDisplayOrder { get; set; }
    }
    public class GetPillarQuestionByCountryRespones 
    {
        public int AssessmentID { get; set; }
        public int UserCountryMappingID { get; set; }
        public int PillarID { get; set; }
        public string PillarName { get; set; }
        public int DisplayOrder { get; set; }
        public string Description { get; set; }
        public int SubmittedPillarDisplayOrder { get; set; }
        public List<AssessmentQuestionResponseDto> Questions { get; set; }
    }
}

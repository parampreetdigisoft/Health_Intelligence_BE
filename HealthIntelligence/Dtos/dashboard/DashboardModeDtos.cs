using HealthIntelligence.Models;

namespace HealthIntelligence.Dtos.dashboard
{
    public class DashboardModeResponseDto
    {
        public int CountryID { get; set; }
        public int DashboardModeID { get; set; }
        public string ModeName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<DashboardQuestionScoreDto> Questions { get; set; } = new();
        public List<DashboardInterpretation> DashboardInterpretations { get; set; } = new();
    }

    public class DashboardQuestionScoreDto
    {
        public int QuestionID { get; set; }
        public string QuestionDescription { get; set; } = string.Empty;
        public decimal? AiScore { get; set; }
        public int? AiTotalScore { get; set; }
        public int? AiTotalAns { get; set; }
        public int? AiTotalNA { get; set; }
        public int? AiTotalUnknown { get; set; }
        public decimal? EvaluationScore { get; set; }
        public int? EvaluationTotalScore { get; set; }
        public int? EvaluationTotalAns { get; set; }
        public int? EvaluationTotalNA { get; set; }
        public int? EvaluationTotalUnknown { get; set; }
        public string? Condition { get; set; }
        public string? Interpretation { get; set; }  
    }

    
}

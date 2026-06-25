using System;

namespace HealthIntelligence.Models
{
    public enum ScoreValue { Four = 100, Three = 75, Two = 50, One = 25, Zero = 0, NA, Unknown }
    public class AssessmentResponse
    {
        public int ResponseID { get; set; }
        public int PillarAssessmentID { get; set; }
        public int QuestionID { get; set; }
        public int QuestionOptionID { get; set; }
        public ScoreValue? Score { get; set; }
        public string Justification { get; set; } 
        public string? Source { get; set; } 
        public PillarAssessment PillarAssessment { get; set; } 
        public Question Question { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
} 
using System.Text.Json.Serialization;

namespace HealthIntelligence.Models
{
    public class Question
    {
        public int QuestionID { get; set; }
        public int PillarID { get; set; }
        public string QuestionText { get; set; }
        public int DisplayOrder { get; set; }
        [JsonIgnore]
        public Pillar Pillar { get; set; }
        public bool IsDeleted { get; set; } = false;
        public ICollection<QuestionOption> QuestionOptions { get; set; } = new List<QuestionOption>();
    }
} 
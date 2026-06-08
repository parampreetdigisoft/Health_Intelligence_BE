namespace HealthIntelligence.Dtos.AiDto
{
    public class AiCountrySummeryDto
    {
        public int CountryID { get; set; }
        public string Continent { get; set; }
        public string CountryName { get; set; }       
        public string Region { get; set; }
        public string? Image { get; set; }
        public int ScoringYear { get; set; }
        public decimal? AIScore { get; set; }
        public decimal? AIProgress { get; set; }
        public decimal? EvaluatorProgress { get; set; }
        public decimal? Discrepancy { get; set; }
        public string ConfidenceLevel { get; set; }
        public string EvidenceSummary { get; set; }
        public string CrossPillarPatterns { get; set; }
        public string InstitutionalCapacity { get; set; }
        public string EquityAssessment { get; set; }
        public string SustainabilityOutlook { get; set; }
        public string StrategicRecommendations { get; set; }
        public string DataTransparencyNote { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsVerified { get; set; }
        public string? Comment { get; set; }
        public decimal? AICompletionRate { get; set; }       
        public int? Rank { get; set; }
        public int? TotalCountry { get; set; }
        public int? RegionRank { get; set; }
        public int? RegionTotalCountry { get; set; }
        public string? ImmediateSituationSummary { get; set; }
        public string? KeyDevelopments { get; set; }
        public string? CriticalRisks { get; set; }
        public string? Gaps { get; set; }
    }
}

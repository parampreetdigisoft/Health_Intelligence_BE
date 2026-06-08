using HealthIntelligence.Dtos.CommonDto;

namespace HealthIntelligence.Dtos.CountryUserDto
{
    public class CompareKpiCountryRequest
    {
        public List<int> Countries { get; set; }
        public DateTime UpdatedAt { get; set; } = new DateTime(DateTime.Now.Year, 1, 1);
    }

    public class CompareCountryRequestDto : PaginationRequest
    {
        public List<int> Countries { get; set; }
        public List<int> Kpis { get; set; } = new();
        public DateTime UpdatedAt { get; set; } = new DateTime(DateTime.Now.Year, 1, 1);
    }

}

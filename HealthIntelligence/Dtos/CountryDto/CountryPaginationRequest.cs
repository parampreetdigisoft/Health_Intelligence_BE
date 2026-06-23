using HealthIntelligence.Dtos.CommonDto;

namespace HealthIntelligence.Dtos.CountryDto
{
    public class CountryPaginationRequest: PaginationRequest
    {
        public int? CountryID { get; set; }
    }
}

using HealthIntelligence.Dtos.CommonDto;

namespace HealthIntelligence.Dtos.PublicDto
{
    public class PartnerCountryRequestDto : PaginationRequest
    {
        public string? Country { get; set; }
        public int? CountryID { get; set; }
        public string? Region { get; set; }
        public int? PillarID { get; set; }
    }
    
}

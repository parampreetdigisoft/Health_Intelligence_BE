using HealthIntelligence.Dtos.CommonDto;

namespace HealthIntelligence.Dtos.PublicDto
{
    public class PartnerCityRequestDto : PaginationRequest
    {
        public string? Country { get; set; }
        public int? CityID { get; set; }
        public string? Region { get; set; }
        public int? PillarID { get; set; }
    }
    
}

using HealthIntelligence.Dtos.CountryDto;

namespace HealthIntelligence.Dtos.UserDtos
{
    public class GetUserByRoleResponse : PublicUserResponse
    {
        public List<AddUpdateCountryDto> countries { get; set; } = new();
    }
}

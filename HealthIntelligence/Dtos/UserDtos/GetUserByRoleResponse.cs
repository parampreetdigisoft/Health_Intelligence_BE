using HealthIntelligence.Dtos.CityDto;

namespace HealthIntelligence.Dtos.UserDtos
{
    public class GetUserByRoleResponse : PublicUserResponse
    {
        public List<AddUpdateCityDto> cities { get; set; } = new();
    }
}

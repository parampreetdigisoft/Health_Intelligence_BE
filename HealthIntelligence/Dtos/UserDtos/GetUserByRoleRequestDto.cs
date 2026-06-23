using HealthIntelligence.Dtos.CommonDto;
using HealthIntelligence.Models;

namespace HealthIntelligence.Dtos.UserDtos
{
    public class GetUserByRoleRequestDto : PaginationRequest
    {
        public UserRole? GetUserRole { get; set; }
        public int UserID { get; set; }
    }
}

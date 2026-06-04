using HealthIntelligence.Common.Models;
using HealthIntelligence.Dtos.AssessmentDto;
using HealthIntelligence.Dtos.CommonDto;
using HealthIntelligence.Dtos.UserDtos;
using HealthIntelligence.Models;

namespace HealthIntelligence.IServices
{
    public interface IUserService
    {
        User GetByEmail(string email);
        Task<PaginationResponse<GetUserByRoleResponse>> GetUserByRoleWithAssignedCity(GetUserByRoleRequestDto requestDto);
        Task<ResultResponseDto<List<PublicUserResponse>>> GetEvaluatorByAnalyst(GetAssignUserDto requestDto);
        Task<ResultResponseDto<List<GetAssessmentResponseDto>>> GetUsersAssignedToCity(int cityId);
        Task<ResultResponseDto<UpdateUserResponseDto>> GetUserInfo(int userId);

    }
} 
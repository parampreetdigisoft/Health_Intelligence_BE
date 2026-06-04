using HealthIntelligence.Common.Models;
using HealthIntelligence.Dtos.AiDto;
using HealthIntelligence.Dtos.AssessmentDto;
using HealthIntelligence.Dtos.CityDto;
using HealthIntelligence.Dtos.CityUserDto;
using HealthIntelligence.Dtos.CommonDto;
using HealthIntelligence.Dtos.kpiDto;
using HealthIntelligence.Dtos.PublicDto;
using HealthIntelligence.Enums;
using HealthIntelligence.Models;

namespace HealthIntelligence.IServices
{
    public interface ICityUserService
    {
        Task<ResultResponseDto<List<PartnerCityResponseDto>>> GetCityUserCities(int userID);
        Task<ResultResponseDto<CityHistoryDto>> GetCityHistory(int userId, TieredAccessPlan tier);
        Task<ResultResponseDto<List<GetCitiesSubmitionHistoryReponseDto>>> GetCitiesProgressByUserId(int userID);
        Task<GetCityQuestionHistoryReponseDto> GetCityQuestionHistory(UserCityRequstDto userCityRequstDto);
        Task<PaginationResponse<CityResponseDto>> GetCitiesAsync(PaginationRequest request);
        Task<ResultResponseDto<CityDetailsDto>> GetCityDetails(UserCityRequstDto userCityRequstDto);
        Task<ResultResponseDto<List<CityPillarQuestionDetailsDto>>> GetCityPillarDetails(UserCityGetPillarInfoRequstDto userCityRequstDto);
        Task<ResultResponseDto<string>> AddCityUserKpisCityAndPillar(AddCityUserKpisCityAndPillar payload,int userID, string tierName);
        Task<ResultResponseDto<List<GetAllKpisResponseDto>>> GetCityUserKpi(int userID, string tierName);
        Task<ResultResponseDto<CompareCityResponseDto>> CompareCities(CompareCityRequestDto c, int userId, string tierName, bool applyPagination = true);
        Task<ResultResponseDto<AiCityPillarReponseDto>> GetAICityPillars(AiCityPillarRequestDto r, int userID, string tierName);
        Task<Tuple<string, byte[]>> ExportCompareCities(CompareKpiCityRequest request, int userId, string tierName);

        Task<ResultResponseDto<List<Pillar>>> GetAllAsync(int userId, UserRole userRole);

    }
}

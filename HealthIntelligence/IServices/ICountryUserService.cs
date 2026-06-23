using HealthIntelligence.Common.Models;
using HealthIntelligence.Dtos.AiDto;
using HealthIntelligence.Dtos.AssessmentDto;
using HealthIntelligence.Dtos.CountryDto;
using HealthIntelligence.Dtos.CommonDto;
using HealthIntelligence.Dtos.kpiDto;
using HealthIntelligence.Dtos.PublicDto;
using HealthIntelligence.Enums;
using HealthIntelligence.Models;
using HealthIntelligence.Dtos.CountryUserDto;

namespace HealthIntelligence.IServices
{
    public interface ICountryUserService
    {
        Task<List<Pillar>> GetAllAsync(int userId, UserRole userRole);
        Task<ResultResponseDto<List<PartnerCountryResponseDto>>> GetCountryUserCountries(int userID);
        Task<ResultResponseDto<CountryHistoryDto>> GetCountryHistory(int userId, TieredAccessPlan tier);
        Task<ResultResponseDto<List<GetCountriesSubmitionHistoryResponseDto>>> GetCountriesProgressByUserId(int userID);
        Task<GetCountryQuestionHistoryResponseDto> GetCountryQuestionHistory(UserCountryRequestDto userCountryRequstDto);
        Task<PaginationResponse<CountryResponseDto>> GetCountriesAsync(PaginationRequest request);
        Task<ResultResponseDto<CountryDetailsDto>> GetCountryDetails(UserCountryRequestDto userCountryRequstDto);
        Task<ResultResponseDto<List<CountryPillarQuestionDetailsDto>>> GetCountryPillarDetails(UserCountryGetPillarInfoRequestDto userCountryGetPillarInfoRequestDto);
        Task<ResultResponseDto<string>> AddCountryUserKpisCountryAndPillar(AddCountryUserKpisCountryAndPillar payload,int userID, string tierName);
        Task<ResultResponseDto<List<GetAllKpisResponseDto>>> GetCountryUserKpi(int userID, string tierName);
        Task<ResultResponseDto<CompareCountryResponseDto>> CompareCountries(CompareCountryRequestDto c, int userId, string tierName, bool applyPagination = true);
        Task<ResultResponseDto<AiCountryPillarResponseDto>> GetAICountryPillars(AiCountryPillarRequestDto r, int userID, string tierName);
        Task<Tuple<string, byte[]>> ExportCompareCountries(CompareCountryRequestDto request, int userId, string tierName);
    }
}

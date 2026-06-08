using HealthIntelligence.Common.Models;
using HealthIntelligence.Dtos.AiDto;
using HealthIntelligence.Dtos.AssessmentDto;
using HealthIntelligence.Dtos.CountryDto;
using HealthIntelligence.Dtos.CountryUserDto;
using HealthIntelligence.Dtos.CommonDto;
using HealthIntelligence.Dtos.kpiDto;
using HealthIntelligence.Dtos.PublicDto;
using HealthIntelligence.Enums;
using HealthIntelligence.Models;

namespace HealthIntelligence.IServices
{
    public interface ICountryUserService
    {
        Task<ResultResponseDto<List<PartnerCountryResponseDto>>> GetCountryUserCountries(int userID);
        Task<ResultResponseDto<CountryHistoryDto>> GetCountryHistory(int userId, TieredAccessPlan tier);
        Task<ResultResponseDto<List<GetCountriesSubmitionHistoryReponseDto>>> GetCountriesProgressByUserId(int userID);
        Task<GetCountryQuestionHistoryReponseDto> GetCountryQuestionHistory(UserCountryRequstDto userCountryRequstDto);
        Task<PaginationResponse<CountryResponseDto>> GetCountriesAsync(PaginationRequest request);
        Task<ResultResponseDto<CountryDetailsDto>> GetCountryDetails(UserCountryRequstDto userCountryRequstDto);
        Task<ResultResponseDto<List<CountryPillarQuestionDetailsDto>>> GetCountryPillarDetails(UserCountryGetPillarInfoRequstDto userCountryRequstDto);
        Task<ResultResponseDto<string>> AddCountryUserKpisCountryAndPillar(AddCountryUserKpisCountryAndPillar payload,int userID, string tierName);
        Task<ResultResponseDto<List<GetAllKpisResponseDto>>> GetCountryUserKpi(int userID, string tierName);
        Task<ResultResponseDto<CompareCountryResponseDto>> CompareCountries(CompareCountryRequestDto c, int userId, string tierName, bool applyPagination = true);
        Task<ResultResponseDto<AiCountryPillarReponseDto>> GetAICountryPillars(AiCountryPillarRequestDto r, int userID, string tierName);
        Task<Tuple<string, byte[]>> ExportCompareCountries(CompareKpiCountryRequest request, int userId, string tierName);

        Task<ResultResponseDto<List<Pillar>>> GetAllAsync(int userId, UserRole userRole);

    }
}

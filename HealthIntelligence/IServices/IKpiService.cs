using HealthIntelligence.Common.Models;
using HealthIntelligence.Dtos.CountryUserDto;
using HealthIntelligence.Dtos.CommonDto;
using HealthIntelligence.Dtos.kpiDto;
using HealthIntelligence.Enums;
using HealthIntelligence.Models;

namespace HealthIntelligence.IServices
{
    public interface IKpiService
    {
        Task<PaginationResponse<GetAnalyticalLayerResultDto>> GetAnalyticalLayerResults(GetAnalyticalLayerRequestDto request, int userId, UserRole role, TieredAccessPlan userPlan = TieredAccessPlan.Pending);
        Task<ResultResponseDto<List<AnalyticalLayer>>> GetAllKpi(int userId, UserRole role);
        Task<ResultResponseDto<CompareCountryResponseDto>> CompareCountries(CompareCountryRequestDto c, int userId, UserRole role, bool applyPagination = true);
        Task<ResultResponseDto<GetMutiplekpiLayerResultsDto>> GetMutiplekpiLayerResults(GetMutiplekpiLayerRequestDto request, int userId, UserRole role, TieredAccessPlan userPlan = TieredAccessPlan.Pending);
        Task<Tuple<string, byte[]>> ExportCompareCountries(CompareKpiCountryRequest request, int userId, UserRole role);

    }
}

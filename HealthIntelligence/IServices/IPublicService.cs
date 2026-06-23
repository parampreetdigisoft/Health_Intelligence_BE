using HealthIntelligence.Common.Models;
using HealthIntelligence.Dtos.chatDto;
using HealthIntelligence.Dtos.CommonDto;
using HealthIntelligence.Dtos.PublicDto;

namespace HealthIntelligence.IServices
{
    public interface IPublicService
    {
        Task<ResultResponseDto<List<PartnerCountryResponseDto>>> getAllCountries();
        Task<ResultResponseDto<PartnerCountryFilterResponse>> GetPartnerCountriesFilterRecord();
        Task<ResultResponseDto<List<PillarResponseDto>>> GetAllPillarAsync();
        Task<PaginationResponse<PartnerCountryResponseDto>> GetPartnerCountries(PartnerCountryRequestDto r);
        Task<CountryCityResponse> GetCountriesAndCountries_WithStaleSupport();
        Task<ResultResponseDto<List<PromotedPillarsResponseDto>>> GetPromotedCountries();
        Task<ResultResponseDto<List<PillarDmiResultDto>>> GetPillarsDmi();
        Task<ResultResponseDto<EmergingTrendsResult>> GetEmergingTrendsAndIssues(int countryCount);
        /// <summary>
        /// Fetches emerging trends from AI, enriches countries, and caches on success only.
        /// </summary>
        Task<bool> RefreshEmergingTrendsCacheAsync(int countryCount, CancellationToken cancellationToken = default);
        Task<ResultResponseDto<PillarLiveSignalsResult>> GetPillarLiveSignals();
    }
}

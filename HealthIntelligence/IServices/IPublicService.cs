using HealthIntelligence.Common.Models;
using HealthIntelligence.Dtos.chatDto;
using HealthIntelligence.Dtos.CommonDto;
using HealthIntelligence.Dtos.PublicDto;

namespace HealthIntelligence.IServices
{
    public interface IPublicService
    {
        Task<ResultResponseDto<List<PartnerCountryResponseDto>>> GetAllCountries();
        Task<ResultResponseDto<PartnerCountryFilterResponse>> GetPartnerCountriesFilterRecord();
        Task<ResultResponseDto<List<PillarResponseDto>>> GetAllPillarAsync();
        Task<PaginationResponse<PartnerCountryResponseDto>> GetPartnerCountries(PartnerCountryRequestDto r);
        Task<CountryCityResponse> GetCountriesAndCities_WithStaleSupport();
        Task<ResultResponseDto<List<PromotedPillarsResponseDto>>> GetPromotedCountries();
        Task<ResultResponseDto<EmergingTrendsResult>> GetEmergingTrendsAndIssues(int countryCount);
        Task<ResultResponseDto<PillarLiveSignalsResult>> GetPillarLiveSignals();
        Task<bool> RefreshEmergingTrendsCacheAsync(int countryCount, CancellationToken cancellationToken = default);
    }
}

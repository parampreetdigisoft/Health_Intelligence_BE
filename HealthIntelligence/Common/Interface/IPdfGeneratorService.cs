using HealthIntelligence.Dtos.AiDto;
using HealthIntelligence.Models;
using HealthIntelligence.Services;

namespace HealthIntelligence.Common.Interface
{
    public interface IPdfGeneratorService
    {

        Task<byte[]> GenerateCountryDetailsPdf(AiCountrySummeryDto country, List<AiCountryPillarReponse> pillars, List<KpiChartItem> kpis, List<PeerCountryHistoryReportDto> peerCountry, UserRole userRole);
        Task<byte[]> GeneratePillarDetailsPdf(AiCountryPillarReponse countryDetails, UserRole userRole);
        Task<byte[]> GenerateAllCountriesDetailsPdf(List<AiCountrySummeryDto> countries, Dictionary<int, List<AiCountryPillarReponse>> pillars, List<KpiChartItem> kpis, UserRole userRole);
    }
}

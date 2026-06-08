using HealthIntelligence.Dtos.AiDto;
using HealthIntelligence.Models;
using HealthIntelligence.Services;

namespace HealthIntelligence.Common.Interface
{
    /// <summary>
    /// Low-level Word document generation contract.
    /// Consumed by <see cref="DocumentGeneratorService"/>;
    /// controllers should depend on <see cref="IDocumentGeneratorService"/> instead.
    /// </summary>
    public interface IDocxGeneratorService
    {
        Task<byte[]> GenerateCountryDetailsDocx(
            AiCountrySummeryDto country,
            List<AiCountryPillarReponse> pillars,
            List<KpiChartItem> kpis,
            List<PeerCountryHistoryReportDto> peerCountries,
            UserRole userRole);

        Task<byte[]> GeneratePillarDetailsDocx(
            AiCountryPillarReponse pillarData,
            UserRole userRole);

        Task<byte[]> GenerateAllCountriesDetailsDocx(
            List<AiCountrySummeryDto> countries,
            Dictionary<int, List<AiCountryPillarReponse>> pillarsDict,
            List<KpiChartItem> kpis,
            UserRole userRole);
    }
}

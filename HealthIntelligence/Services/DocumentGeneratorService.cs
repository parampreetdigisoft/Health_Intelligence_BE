using HealthIntelligence.Common.Interface;
using HealthIntelligence.Dtos.AiDto;
using HealthIntelligence.Models;
using HealthIntelligence.Services;

namespace HealthIntelligence.Common.Implementation
{
    /// <summary>
    /// Facade that delegates to <see cref="PdfGeneratorService"/> or
    /// <see cref="DocxGeneratorService"/> based on the requested <see cref="DocumentFormat"/>.
    ///
    /// Register as: services.AddScoped&lt;IDocumentGeneratorService, DocumentGeneratorService&gt;()
    /// </summary>
    public sealed class DocumentGeneratorService : IDocumentGeneratorService
    {
        private readonly IPdfGeneratorService _pdf;
        private readonly IDocxGeneratorService _docx;

        public DocumentGeneratorService(
            IPdfGeneratorService pdf,
            IDocxGeneratorService docx)
        {
            _pdf = pdf;
            _docx = docx;
        }

        public Task<byte[]> GenerateCountryDetails(
            AiCountrySummeryDto country,
            List<AiCountryPillarReponse> pillars,
            List<KpiChartItem> kpis,
            List<PeerCountryHistoryReportDto> peerCountry,
            UserRole userRole,
        Interface.DocumentFormat format = Interface.DocumentFormat.Pdf)
        {
             var result = format == Interface.DocumentFormat.Docx
                ? _docx.GenerateCountryDetailsDocx(country, pillars, kpis, peerCountry, userRole)
                : _pdf.GenerateCountryDetailsPdf(country, pillars, kpis, peerCountry, userRole);

            return result;
        }

        public Task<byte[]> GeneratePillarDetails(
            AiCountryPillarReponse pillarData,
            UserRole userRole,
            Interface.DocumentFormat format = Interface.DocumentFormat.Pdf)
            => format == Interface.DocumentFormat.Docx
                ? _docx.GeneratePillarDetailsDocx(pillarData, userRole)
                : _pdf.GeneratePillarDetailsPdf(pillarData, userRole);

        public Task<byte[]> GenerateAllCountriesDetails(
            List<AiCountrySummeryDto> countries,
            Dictionary<int, List<AiCountryPillarReponse>> pillarsDict,
            List<KpiChartItem> kpis,
            UserRole userRole,
            Interface.DocumentFormat format = Interface.DocumentFormat.Pdf)
            => format == Interface.DocumentFormat.Docx
                ? _docx.GenerateAllCountriesDetailsDocx(countries, pillarsDict, kpis, userRole)
                : _pdf.GenerateAllCountriesDetailsPdf(countries, pillarsDict, kpis, userRole);
    }
}

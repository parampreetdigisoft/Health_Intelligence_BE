
using AssessmentPlatform.Dtos.AiDto;
using AssessmentPlatform.Models;
using HealthIntelligence.Common.Interface;
using HealthIntelligence.Dtos.AiDto;
using HealthIntelligence.IServices;
using HealthIntelligence.Models;
using static HealthIntelligence.Services.AIComputationService;


namespace HealthIntelligence.Services
{
    /// <summary>
    /// Facade that delegates to <see cref="PdfGeneratorService"/> or
    /// <see cref="DocxGeneratorService"/> based on the requested <see cref="DocumentFormat"/>.
    ///
    /// Register as: services.AddScoped&lt;IDocumentGeneratorService, DocumentGeneratorService&gt;()
    /// </summary>
    public sealed class DocumentGeneratorService : IDocumentGeneratorService
    {
        private readonly Common.Interface.IPdfGeneratorService _pdf;
        private readonly IDocxGeneratorService _docx;

        public DocumentGeneratorService(
            Common.Interface.IPdfGeneratorService pdf,
            IDocxGeneratorService docx)
        {
            _pdf = pdf;
            _docx = docx;
        }

        public Task<byte[]> GenerateCountryDetails(
            AiCountrySummeryDto country,
            List<AiCountryPillarResponse> pillars,
            List<KpiChartItem> kpis,
            List<PeerCountryHistoryReportDto> peercountry,
            UserRole userRole,
        HealthIntelligence.IServices.DocumentFormat format = HealthIntelligence.IServices.DocumentFormat.Pdf)
        {
             var result = format == HealthIntelligence.IServices.DocumentFormat.Docx
                ? _docx.GenerateCountryDetailsDocx(country, pillars, kpis, peercountry, userRole)
                : _pdf.GenerateCountryDetailsPdf(country, pillars, kpis, peercountry, userRole);

            return result;
        }

        public Task<byte[]> GeneratePillarDetails(
            AiCountryPillarResponse pillarData,
            UserRole userRole,
            HealthIntelligence.IServices.DocumentFormat format = HealthIntelligence.IServices.DocumentFormat.Pdf)
            => format == HealthIntelligence.IServices.DocumentFormat.Docx
                ? _docx.GeneratePillarDetailsDocx(pillarData, userRole)
                : _pdf.GeneratePillarDetailsPdf(pillarData, userRole);

        public Task<byte[]> GenerateAllCountriesDetails(
            List<AiCountrySummeryDto> countries,
            Dictionary<int, List<AiCountryPillarResponse>> pillarsDict,
            List<KpiChartItem> kpis,
            UserRole userRole,
            HealthIntelligence.IServices.DocumentFormat format = HealthIntelligence.IServices.DocumentFormat.Pdf)
            => format == HealthIntelligence.IServices.DocumentFormat.Docx
                ? _docx.GenerateAllCountriesDetailsDocx(countries, pillarsDict, kpis, userRole)
                : _pdf.GenerateAllCountriesDetailsPdf(countries, pillarsDict, kpis, userRole);
    }
}

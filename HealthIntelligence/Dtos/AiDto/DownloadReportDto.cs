namespace HealthIntelligence.Dtos.AiDto
{
    public class DownloadReportDto
    {
        public List<int> CountryIDs { get; set; }
        public Common.Interface.DocumentFormat Format { get; set; } = Common.Interface.DocumentFormat.Pdf;

    }
}

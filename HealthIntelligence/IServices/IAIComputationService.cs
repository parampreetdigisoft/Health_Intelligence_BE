using HealthIntelligence.Common.Models;
using HealthIntelligence.Dtos.AiDto;
using HealthIntelligence.Dtos.CommonDto;
using HealthIntelligence.Models;
using Microsoft.AspNetCore.Mvc;


namespace HealthIntelligence.IServices
{
    public interface IAIComputationService
    {
        Task<ResultResponseDto<List<AITrustLevel>>> GetAITrustLevels();
        Task<PaginationResponse<AiCountrySummeryDto>> GetAICountries(AiCountrySummeryRequestDto request, int userID, UserRole userRole);
        Task<ResultResponseDto<AiCountryPillarReponseDto>> GetAICountryPillars(int countryID, int userID, UserRole userRole,int year=0);
        Task<PaginationResponse<AIEstimatedQuestionScoreDto>> GetAIPillarsQuestion(AiCountryPillarSummeryRequestDto r, int userID, UserRole userRole);
        Task<IQueryable<AiCountrySummeryDto>> GetCountryAiSummeryDetails(int userID, UserRole userRole, int? countryId, int year=0);
        Task<byte[]> GenerateCountryDetailsReport(AiCountrySummeryDto countryDetails, UserRole userRole, int userID, Common.Interface.DocumentFormat format = Common.Interface.DocumentFormat.Pdf, string reportType = "AI");
        Task<byte[]> GeneratePillarDetailsReport(AiCountryPillarReponse countryDetails, UserRole userRole, Common.Interface.DocumentFormat format = Common.Interface.DocumentFormat.Pdf);
        Task<ResultResponseDto<AiCrossCountryResponseDto>> GetAICrossCountryPillars(AiCountryIdsDto ids, int userID, UserRole userRole);
        Task<ResultResponseDto<bool>> ChangedAiCountryEvaluationStatus(ChangedAiCountryEvaluationStatusDto aiCountryIdsDto, int userID, UserRole userRole);
        Task<ResultResponseDto<bool>> RegenerateAiSearch(RegenerateAiSearchDto aiCountryIdsDto, int userID, UserRole userRole);
        Task<ResultResponseDto<bool>> AddComment(AddCommentDto aiCountryIdsDto, int userID, UserRole userRole);
        Task<ResultResponseDto<bool>> RegeneratePillarAiSearch(RegeneratePillarAiSearchDto aiCountryIdsDto, int userID, UserRole userRole);
        Task<AiCountrySummeryDto> GetCountryAiSummeryDetail(int userID, UserRole userRole, int? countryID, int year, string reportType = "AI");
        Task<List<AiCountrySummeryDto>> GetAllCountryAiSummeryDetail(int userID, UserRole userRole, int year);
        Task<byte[]> GenerateAllCountryDetailsReport(List<AiCountrySummeryDto> countryDetails, UserRole userRole, int userID, int year, Common.Interface.DocumentFormat format = Common.Interface.DocumentFormat.Pdf);
        public Task<ResultResponseDto<Dictionary<int, List<AiCountryPillarReponse>>>> GetAllCountriesAIPillars(int userID, UserRole userRole, int currentYear = 0);
        Task<ResultResponseDto<string>> AITransferAssessment(AITransferAssessmentRequestDto r, int userID, UserRole userRole);
        Task<ResultResponseDto<string>> ReCalculateKpis(int userID, UserRole userRole);

        Task<ResultResponseDto<string>> UploadAiDocuments(UploadAiDocumentRequest r, int userID, UserRole userRole);
        Task<PaginationResponse<GetCountryDocumentResponseDto>> GetAICountryDocuments(AiCountryDocumentRequestDto request, int userID, UserRole userRole);
        Task<ResultResponseDto<List<GetCountryPillarDocumentResponseDto>>> GetAICountryPillarDocuments(AiCountryPillarDocumentRequestDto request, int userID, UserRole userRole);
        Task<ResultResponseDto<string>> DeleteDocument(DeleteCountryDocumentRequestDto request, int userID, UserRole userRole);
        Task<FileResult> DownloadDocument(int countryDocumentID, int userID, UserRole userRole);

    }
}

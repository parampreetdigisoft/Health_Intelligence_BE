using HealthIntelligence.Common.Models;
using HealthIntelligence.Dtos.AssessmentDto;
using HealthIntelligence.Dtos.CommonDto;
using HealthIntelligence.Dtos.dashboard;
using HealthIntelligence.Models;

namespace HealthIntelligence.IServices
{
    public interface IAssessmentResponseService
    {
        Task<List<AssessmentResponse>> GetAllAsync();
        Task<AssessmentResponse> GetByIdAsync(int id);
        Task<AssessmentResponse> AddAsync(AssessmentResponse response);
        Task<AssessmentResponse> UpdateAsync(int id, AssessmentResponse response);
        Task<bool> DeleteAsync(int id);
        Task<ResultResponseDto<string>> SaveAssessment(AddAssessmentDto request);
        Task<PaginationResponse<GetCountryAssessmentResponseDto>> GetAssessmentResult(GetAssessmentRequestDto request, UserRole role);
        Task<PaginationResponse<GetAssessmentQuestionResponseDto>> GetAssessmentQuestion(GetAssessmentQuestoinRequestDto request);
        Task<ResultResponseDto<string>> ImportAssessmentAsync(IFormFile file,int userID);
        Task<GetCountryQuestionHistoryReponseDto> GetCountryQuestionHistory(UserCountryRequstDto userCountryRequstDto);
        Task<ResultResponseDto<GetAssessmentHistoryDto>> GetAssessmentProgressHistory(int assessmentID);
        Task<ResultResponseDto<string>> ChangeAssessmentStatus(ChangeAssessmentStatusRequestDto r);
        Task<ResultResponseDto<string>> TransferAssessment(TransferAssessmentRequestDto r, int userID, UserRole userRole);
        Task<ResultResponseDto<AiCountryPillarDashboardResponseDto>> GetCountryPillarHistory(UserCountryDashBoardRequstDto userCountryRequstDto,int userID, UserRole userRole);
    }
} 
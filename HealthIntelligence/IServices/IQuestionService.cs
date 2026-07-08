using HealthIntelligence.Common.Models;
using HealthIntelligence.Dtos.AssessmentDto;
using HealthIntelligence.Dtos.CommonDto;
using HealthIntelligence.Dtos.PillarDto;
using HealthIntelligence.Dtos.QuestionDto;
using HealthIntelligence.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HealthIntelligence.IServices
{
    public interface IQuestionService
    {
        Task<List<GetPillarDto>> GetPillarsAsync();
        Task<PaginationResponse<GetQuestionResponse>> GetQuestionsAsync(GetQuestionRequestDto requestDto);
        Task<Question> AddQuestionAsync(Question q);
        Task<ResultResponseDto<string>> AddUpdateQuestion(AddUpdateQuestionDto q);
        Task<ResultResponseDto<string>> AddBulkQuestion(AddBulkQuestionsDto q);
        Task<Question> EditQuestionAsync(int id, Question q);
        Task<bool> DeleteQuestionAsync(int id);
        Task<ResultResponseDto<GetPillarQuestionByCountryResponse>> GetQuestionsByCountryIdAsync(CountryPillerRequestDto request, int userId);
        Task<Tuple<string,byte[]>> ExportAssessment(int userCountryMappingID, int userId, UserRole role);
        Task<ResultResponseDto<List<QuestionsByUserPillarsResponsetDto>>> GetQuestionsHistoryByPillar(GetCountryPillarHistoryRequestDto requestDto, UserRole role);
        Task<ResultResponseDto<GetPillarQuestionByCountryResponse>> GetQuestionsByCountryMappingIdForAnalyst(CountryPillerRequestDto request, int userId);
    }
} 
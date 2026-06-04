

using HealthIntelligence.Common.Models;
using HealthIntelligence.Dtos.chatDto;
using HealthIntelligence.Models;
using HealthIntelligence.Services;


namespace HealthIntelligence.IServices
{
    public interface IChatService
    {
        Task<ResultResponseDto<List<AIAssistantFAQDto>>> GetAssistantFAQDs(int userId, UserRole userRole);
        Task<ResultResponseDto<ChatResponseDto>> AskAboutCity(CityChatRequestDto request);
        Task<ResultResponseDto<ChatResponseDto>> AskAboutGlobal(ChatGlobalAskQuestionRequestDto request);
        Task<ResultResponseDto<ChatCityExecutiveSlidesResponse>> GetCitySlides(int cityId, int userId, UserRole userRole);

        Task<ResultResponseDto<ChatResponseDto>> CrossComparision(CrossComparisionRequestDto request);
    }
}

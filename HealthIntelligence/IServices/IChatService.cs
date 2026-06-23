using HealthIntelligence.Common.Models;
using HealthIntelligence.Dtos.chatDto;
using HealthIntelligence.Models;

namespace HealthIntelligence.IServices
{
    public interface IChatService
    {
        Task<ResultResponseDto<List<AIAssistantFAQDto>>> GetAssistantFAQDs(int userId, UserRole userRole);
        Task<ResultResponseDto<ChatResponseDto>> AskAboutCountry(CountryChatRequestDto request, int userId, UserRole userRole);
        Task<ResultResponseDto<ChatResponseDto>> AskAboutGlobal(ChatGlobalAskQuestionRequestDto request, int userId, UserRole userRole);
        Task<ResultResponseDto<ChatResponseDto>> CrossComparision(CrossComparisionRequestDto request, int userId, UserRole userRole);
        Task<ResultResponseDto<ChatCountryExecutiveSlidesResponse>> GetCountrySlides(int countryId, int userId, UserRole userRole);

    }
}

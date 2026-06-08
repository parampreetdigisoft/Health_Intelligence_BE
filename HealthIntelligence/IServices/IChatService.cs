

using HealthIntelligence.Common.Models;
using HealthIntelligence.Dtos.chatDto;
using HealthIntelligence.Models;
using HealthIntelligence.Services;


namespace HealthIntelligence.IServices
{
    public interface IChatService
    {
        Task<ResultResponseDto<List<AIAssistantFAQDto>>> GetAssistantFAQDs(int userId, UserRole userRole);
        Task<ResultResponseDto<ChatResponseDto>> AskAboutCountry(CountryChatRequestDto request);
        Task<ResultResponseDto<ChatResponseDto>> AskAboutGlobal(ChatGlobalAskQuestionRequestDto request);
        Task<ResultResponseDto<ChatCountryExecutiveSlidesResponse>> GetCountrySlides(int countryId, int userId, UserRole userRole);

        Task<ResultResponseDto<ChatResponseDto>> CrossComparision(CrossComparisionRequestDto request);
    }
}

using HealthIntelligence.Dtos.chatDto;
using HealthIntelligence.Dtos.PublicDto;
using HealthIntelligence.Services;

namespace HealthIntelligence.IServices
{
    public interface IAIAnalyzeService
    {
        Task AnalyzeAllCountriesFull();
        Task AnalyzeSingleCountryFull(int countryId);
        Task AnalyzeSingleCountry(int countryId);
        Task AnalyzeCountryPillars(int countryId);
        Task AnalyzeSinglePillar(int countryId, int pillarId);
        Task AnalyzeQuestionsOfCountry(int countryId);
        Task AnalyzeQuestionsOfCountryPillar(int countryId, int pillarId);
        Task ProcessDocument(int documentID);
        Task DeleteDocument(int documentID);
        Task AnalyzeCountryImmediateSituation(int countryId);
        Task AnalyzeCountryMissingQuestions(MissingCountryQuestionRequest request);
        Task<ChatCountryAskQuestionResponse> ChatCountryAsk(ChatCountryAskQuestionRequest request);
        Task<ChatCountryAskQuestionResponse> ChatGlobalAsk(ChatGlobalAskQuestionRequest request);
        Task<ChatCountryAskQuestionResponse> CrossComparision(CrossComparisionRequest request);
        Task<ChatEmergingTrendsResponse?> GetEmergingTrendsAndIssues(int country_count);
        Task<ChatCountryExecutiveSlidesResponse?> GetCountrySlides(int countryId);
        Task<ChatPillarLiveSignalsResponse?> GetPillarLiveSignals();
        Task RunEvery2HoursJob();
        Task RunMonthlyJob();
        Task RunDailyJob();        
    }
}

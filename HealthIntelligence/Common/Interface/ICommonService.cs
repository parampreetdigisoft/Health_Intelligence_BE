using HealthIntelligence.Common.Models.views;
using HealthIntelligence.Dtos.CountryDto;
using HealthIntelligence.Dtos.PillarDto;
using HealthIntelligence.Models;

namespace HealthIntelligence.Common.Interface
{
    public interface ICommonService
    {
        Task<List<EvaluationCountryProgressResultDto>> GetCountriesProgressAsync(int userId,int role, int year, int countryID = 0);
        Task<List<EvaluationCountryProgressHistoryResultDto>> GetCountriesProgressHistoryAsync(int userId, int role, int fromYear, int toYear);
        Task<List<GetCountriesProgressAdminDto>> GetCountriesProgressForAdmin(int userId, int role, int year);
        Task<List<CountryRankingResultDto>> GetCountriesRankings(int countryId, int year);
        Task<List<GetPillarDto>> GetPillars();
        void ClearPillarCache();
        Task<List<GetDashboardModeResult>> GetDashboardModeResults(int userId, int role, int dashboardModeID, int countryID = 0);
    }
}

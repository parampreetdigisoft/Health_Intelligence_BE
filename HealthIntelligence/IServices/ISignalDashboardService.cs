using HealthIntelligence.Common.Models;
using HealthIntelligence.Dtos.dashboard;
using HealthIntelligence.Models;

namespace HealthIntelligence.IServices
{
    public interface ISignalDashboardService
    {
        Task<ResultResponseDto<DashboardModeResponseDto>> GetPeaceStressTestDashboard(int countryID, int userId, UserRole userRole);
        Task<ResultResponseDto<DashboardModeResponseDto>> GetEarlyWarningDashboard(int countryID, int userId, UserRole userRole);
        Task<ResultResponseDto<DashboardModeResponseDto>> GetResilienceScorecard(int countryID, int userId, UserRole userRole);
    }
}

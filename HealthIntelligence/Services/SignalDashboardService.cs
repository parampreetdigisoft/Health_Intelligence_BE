using HealthIntelligence.Common.Implementation;
using HealthIntelligence.Common.Interface;
using HealthIntelligence.Common.Models;
using HealthIntelligence.Common.Models.views;
using HealthIntelligence.Data;
using HealthIntelligence.Dtos.dashboard;
using HealthIntelligence.IServices;
using HealthIntelligence.Models;
using Microsoft.EntityFrameworkCore;

namespace HealthIntelligence.Services
{
    public class SignalDashboardService : ISignalDashboardService
    {
        private const int HealthStressTestModeId = 1;
        private const int EarlyWarningModeId = 2;
        private const int ResilienceModeId = 3;

        private readonly ApplicationDbContext _context;
        private readonly IAppLogger _appLogger;
        private readonly ICommonService _commonService;

        public SignalDashboardService(ApplicationDbContext context, IAppLogger appLogger, ICommonService commonService)
        {
            _context = context;
            _appLogger = appLogger;
            _commonService = commonService;
        }

        public Task<ResultResponseDto<DashboardModeResponseDto>> GetPeaceStressTestDashboard(int countryID, int userId, UserRole userRole)
            => GetDashboardMode(HealthStressTestModeId, countryID, userId, userRole, "Health stress test dashboard generated successfully.");

        public Task<ResultResponseDto<DashboardModeResponseDto>> GetEarlyWarningDashboard(int countryID, int userId, UserRole userRole)
            => GetDashboardMode(EarlyWarningModeId, countryID, userId, userRole, "Early warning dashboard generated successfully.");

        public Task<ResultResponseDto<DashboardModeResponseDto>> GetResilienceScorecard(int countryID, int userId, UserRole userRole)
            => GetDashboardMode(ResilienceModeId, countryID, userId, userRole, "Resilience scorecard generated successfully.");

        private async Task<ResultResponseDto<DashboardModeResponseDto>> GetDashboardMode(
            int dashboardModeId,
            int countryID,
            int userId,
            UserRole userRole,
            string successMessage)
        {
            try
            {
                if (userRole == UserRole.CountryUser && !await ValidateCountryAccess(countryID, userId))
                {
                    return ResultResponseDto<DashboardModeResponseDto>.Failure(new[] { "You don't have access to this country data." });
                }

                var dashboardMode = await _context.DashboardModes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.DashboardModeID == dashboardModeId);

                if (dashboardMode == null)
                {
                    return ResultResponseDto<DashboardModeResponseDto>.Failure(new[] { "Dashboard configuration not found." });
                }

                var mappings = await LoadActiveMappings(dashboardModeId);
                if (!mappings.Any())
                {
                    return ResultResponseDto<DashboardModeResponseDto>.Failure(new[] { "Dashboard KPI mappings not found." });
                }

                var interpretations = await _context.DashboardInterpretations
                    .AsNoTracking()
                    .Where(x => x.DashboardModeID == dashboardModeId)
                    .ToListAsync();

                var spResults = await _commonService.GetDashboardModeResults(userId, (int)userRole, dashboardModeId, countryID);
                var spResultsByQuestion = spResults
                    .Where(x => x.QuestionID.HasValue)
                    .GroupBy(x => x.QuestionID!.Value)
                    .ToDictionary(g => g.Key, g => g.First());



                var isCountryUser = userRole == UserRole.CountryUser;

                var Questions = mappings.Select(q => MapQuestionScore(q, spResultsByQuestion, interpretations, isCountryUser)).ToList();


                var response = new DashboardModeResponseDto
                {
                    CountryID = countryID,
                    DashboardModeID = dashboardModeId,
                    ModeName = dashboardMode.ModeName,
                    Description = dashboardMode.Description,
                    Questions = Questions,
                    DashboardInterpretations = interpretations

                };

                return ResultResponseDto<DashboardModeResponseDto>.Success(response, new[] { successMessage });
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync($"Error in GetDashboardMode for mode {dashboardModeId}", ex);
                return ResultResponseDto<DashboardModeResponseDto>.Failure(new[] { "There is an error, please try later" });
            }
        }

        private static DashboardQuestionScoreDto MapQuestionScore(
            DashboardModeKPIMapping question,
            IReadOnlyDictionary<int, GetDashboardModeResult> spResultsByQuestion,
            IReadOnlyList<DashboardInterpretation> interpretations,
            bool isCountryUser)
        {
            var hasData = spResultsByQuestion.TryGetValue(question.QuestionID, out var result);
            var dto = new DashboardQuestionScoreDto
            {
                QuestionID = question.QuestionID,
                QuestionDescription = question.Description ?? ""
            };

            if (!hasData)
            {
                return dto;
            }

            dto.AiScore = result!.AiQuestionScore;
            dto.AiTotalScore = result.AiTotalScore;
            dto.AiTotalAns = result.AiTotalAns;
            dto.AiTotalNA = result.AiTotalNA;
            dto.AiTotalUnknown = result.AiTotalUnknown;

            if (!isCountryUser)
            {
                dto.EvaluationScore = result.QuestionScore;
                dto.EvaluationTotalScore = result.TotalScore;
                dto.EvaluationTotalAns = result.TotalAns;
                dto.EvaluationTotalNA = result.TotalNA;
                dto.EvaluationTotalUnknown = result.TotalUnknown;
            }

            var scoreForInterpretation = isCountryUser ? result.AiQuestionScore : result.QuestionScore;
            ApplyInterpretation(dto, interpretations, scoreForInterpretation.GetValueOrDefault());

            return dto;
        }

        private static void ApplyInterpretation(DashboardQuestionScoreDto dto, IReadOnlyList<DashboardInterpretation> interpretations, decimal score)
        {
            var interpretation = MatchInterpretation(interpretations, score);
            dto.Condition = interpretation?.Condition ?? CommonStaticMethods.GetConditionByScore(score);
            dto.Interpretation = interpretation?.Description;
        }

        private static DashboardInterpretation? MatchInterpretation(IReadOnlyList<DashboardInterpretation> interpretations, decimal score)
        {
            return interpretations.FirstOrDefault(x =>
                (!x.MinRange.HasValue || score >= x.MinRange.Value) &&
                (!x.MaxRange.HasValue || score <= x.MaxRange.Value));
        }

        private async Task<bool> ValidateCountryAccess(int countryID, int userId)
        {
            return await _context.PublicUserCountryMappings
                .AsNoTracking()
                .AnyAsync(x => x.UserID == userId && x.CountryID == countryID && x.IsActive);
        }

        private async Task<List<DashboardModeKPIMapping>> LoadActiveMappings(int dashboardModeId)
        {
            return await _context.DashboardModeKPIMappings
                .AsNoTracking()
                .Where(x => x.DashboardModeID == dashboardModeId && x.IsActive && !x.IsDeleted)
                .ToListAsync();
        }
    }
}

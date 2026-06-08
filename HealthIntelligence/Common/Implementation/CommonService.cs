using HealthIntelligence.Common.Interface;
using HealthIntelligence.Data;
using HealthIntelligence.Dtos.CountryDto;
using HealthIntelligence.IServices;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HealthIntelligence.Common.Implementation
{
    public class CommonService : ICommonService
    {
        #region constructor

        private readonly ApplicationDbContext _context;
        private readonly IAppLogger _appLogger;
        private readonly IWebHostEnvironment _env;
        public CommonService(ApplicationDbContext context, IAppLogger appLogger, IWebHostEnvironment env)
        {
            _context = context;
            _appLogger = appLogger;
            _env = env;
        }
        #endregion

        public static string InitailLineOfExecutiveSummery(
          string evidenceSummary,
          string? immediateSituationSummary,
          decimal? progress,
          string? countryName = "The country", int pillarCount = 14, int kpiCount = 110)
        {
            immediateSituationSummary = immediateSituationSummary ?? "";

            var evidenceSummaryStaringLine = $"{countryName ?? "The country"} records an overall AHSIP score of {progress ?? 0}, reflecting performance across {pillarCount} pillars and {kpiCount} KPIs.";

            return immediateSituationSummary + "\n\n " + evidenceSummaryStaringLine + " " + evidenceSummary;
        }
        public async Task<List<EvaluationCountryProgressResultDto>> GetCountriesProgressAsync(int userId, int role, int year)
        {
            try
            {
                return await _context.CountryProgressResults
                 .FromSqlRaw(
                     "EXEC usp_getCountriesProgressByUserId @userID, @role, @year",
                     new SqlParameter("@userID", userId),
                     new SqlParameter("@role", role),
                     new SqlParameter("@year", year)
                 )
                 .AsNoTracking()
                 .ToListAsync();
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error in Executing usp_getCountriesProgressByUserId", ex);
                return new List<EvaluationCountryProgressResultDto>();
            }
        }
        public async Task<List<EvaluationCountryProgressHistoryResultDto>> GetCountriesProgressHistoryAsync(int userId, int role, int fromYear, int toYear)
        {
            try
            {
                return await _context.CountryProgressHistoryResults
                 .FromSqlRaw(
                     "EXEC usp_getCountriesProgressByUserIdHistory @userID, @role, @fromYear, @toYear",
                     new SqlParameter("@userID", userId),
                     new SqlParameter("@role", role),
                     new SqlParameter("@fromYear", fromYear),
                     new SqlParameter("@toYear", toYear)
                 )
                 .AsNoTracking()
                 .ToListAsync();
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error in Executing usp_getCountriesProgressByUserIdHistory", ex);
                return new List<EvaluationCountryProgressHistoryResultDto>();
            }
        }
        public async Task<List<GetCountriesProgressAdminDto>> GetCountriesProgressForAdmin(int userId, int role, int year)
        {
            try
            {
                return await _context.GetCountriesProgressAdminDto
                 .FromSqlRaw("EXEC usp_getCountriesProgress_Admin @year",new SqlParameter("@year", year))
                 .AsNoTracking()
                 .ToListAsync();
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error in Executing usp_getCountriesProgress_Admin", ex);
                return new List<GetCountriesProgressAdminDto>();
            }
        }
        public string ReplacePercentAcross(string input, int score)
        {
            const string target = " percent across";

            int idx = input.IndexOf(target, StringComparison.OrdinalIgnoreCase);
            if (idx == -1)
                return input;

            // Walk backward to find start of number
            int start = idx - 1;
            while (start >= 0 && char.IsDigit(input[start]))
                start--;

            start++; // move to first digit

            // If no digits found, return original
            if (start >= idx)
                return input;

            return string.Concat(
                input.AsSpan(0, start),
                score.ToString(),
                input.AsSpan(idx)
            );
        }

        public async Task<List<CountryRankingResultDto>> GetCountriesRankings(int countryId, int year)
        {
            try
            {
                return await _context.CountryRankingResultDto
                 .FromSqlRaw(
                     "EXEC usp_getCountryRanking @CountryId, @Year",
                     new SqlParameter("@CountryId", countryId),
                     new SqlParameter("@Year", year)
                 )
                 .AsNoTracking()
                 .ToListAsync();
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error in Executing usp_getCountryRanking", ex);
                return new List<CountryRankingResultDto    >();
            }
        }

    }
}

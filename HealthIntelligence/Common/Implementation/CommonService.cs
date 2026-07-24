using HealthIntelligence.Common.Interface;
using HealthIntelligence.Common.Models.settings;
using HealthIntelligence.Common.Models.views;
using HealthIntelligence.Data;
using HealthIntelligence.Dtos.CountryDto;
using HealthIntelligence.Dtos.PillarDto;
using HealthIntelligence.IServices;
using HealthIntelligence.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace HealthIntelligence.Common.Implementation
{
    public class CommonService : ICommonService
    {
        #region constructor
        private readonly IMemoryCache _memoryCache;
        private const string PILLAR_CACHE_KEY = "PILLAR_CACHE";

        private readonly ApplicationDbContext _context;
        private readonly IAppLogger _appLogger;
        private readonly IWebHostEnvironment _env;
        private readonly AppSettings _appSettings;
        public CommonService(
            ApplicationDbContext context,
            IAppLogger appLogger,
            IWebHostEnvironment env,
            IOptions<AppSettings> appSettings,
            IMemoryCache memoryCache)
        {
            _context = context;
            _appLogger = appLogger;
            _env = env;
            _appSettings = appSettings.Value;
            _memoryCache = memoryCache;
        }

        #endregion

        public static string CountryScoreSummery(decimal? progress,string? countryName = "The country", int pillarCount = 23, int kpiCount = 37)
        {
            var evidenceSummaryStaringLine = $"{countryName ?? "The country"} records an overall AHI score of {progress ?? 0}, reflecting performance across {pillarCount} domains and {kpiCount} KPIs.";

            return evidenceSummaryStaringLine;
        }

        public static string InitailLineOfExecutiveSummery(
            string evidenceSummary,
            string? immediateSituationSummary,
            decimal? progress,
            string? countryName = "The country", int pillarCount = 23, int kpiCount = 37)
        {
            immediateSituationSummary = immediateSituationSummary ?? "";

            var evidenceSummaryStaringLine= $"{countryName ?? "The country"} records an overall AHI score of {progress ?? 0}, reflecting performance across {pillarCount} domains and {kpiCount} KPIs.";

            return immediateSituationSummary + "\n\n " + evidenceSummaryStaringLine + " " + evidenceSummary;
        }


        public async Task<List<EvaluationCountryProgressResultDto>> GetCountriesProgressAsync(int userId, int role, int year, int countryID = 0)
        {
            try
            {
                return await _context.CountryProgressResults
                 .FromSqlRaw(
                     "EXEC usp_getCountriesProgressByUserId @userID, @role, @year", "@countryID",
                     new SqlParameter("@userID", userId),
                     new SqlParameter("@role", role),
                     new SqlParameter("@year", year),
                     new SqlParameter("@countryID", countryID)
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

        public async Task<List<CountryRankingResultDto>> GetCountriesRankings(int countryId, int year)
        {
            try
            {
                return await _context.CountryRankingResults
                 .FromSqlRaw(
                     "EXEC usp_getCountryRanking @countryId, @year",
                     new SqlParameter("@countryId", countryId),
                     new SqlParameter("@year", year)
                 )
                 .AsNoTracking()
                 .ToListAsync();
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error in Executing usp_getCountryRanking", ex);
                return new List<CountryRankingResultDto>();
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


        public async Task<List<GetPillarDto>> GetPillars()
        {
            try
            {
                if (_memoryCache.TryGetValue(PILLAR_CACHE_KEY, out List<GetPillarDto> pillars))
                {
                    return pillars;
                }

                pillars = await _context.Pillars
                    .Where(x => x.IsActive && !x.IsDeleted)
                    .OrderBy(x=>x.DisplayOrder)
                    .Select(x=> new GetPillarDto
                    {
                        PillarID = x.PillarID,
                        PillarName = x.PillarName,
                        Description = x.Description,
                        DisplayOrder = x.DisplayOrder,
                        ImagePath = x.ImagePath,
                        Weight = x.Weight,
                        Reliability = x.Reliability,
                        PillarCode = x.PillarCode,
                        IsActive = x.IsActive,
                        QuestionCount = x.Questions.Where(x=>!x.IsDeleted).Count()
                    })
                    .ToListAsync();

                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(1));

                _memoryCache.Set(PILLAR_CACHE_KEY, pillars, cacheOptions);

                return pillars;
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error in GetPillars", ex);
                return new List<GetPillarDto>();
            }
        }
        public void ClearPillarCache()
        {
            _memoryCache.Remove(PILLAR_CACHE_KEY);
        }

        public async Task<List<GetDashboardModeResult>> GetDashboardModeResults(int userId, int role, int dashboardModeID, int countryID = 0)
        {
            try
            {
                var result = await _context.GetDashboardModeResults
                 .FromSqlRaw(
                     "EXEC usp_getDashboardModeResult @userID, @role, @DashboardModeID, @countryID",
                     new SqlParameter("@userID", userId),
                     new SqlParameter("@role", role),
                     new SqlParameter("@DashboardModeID", dashboardModeID),
                     new SqlParameter("@countryID", countryID)
                 )
                 .AsNoTracking()
                 .ToListAsync();

                return result ?? new List<GetDashboardModeResult>();
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error in Executing usp_getDashboardModeResult", ex);
                return new List<GetDashboardModeResult>();
            }
        }
    }
}

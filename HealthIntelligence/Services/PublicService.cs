using HealthIntelligence.Common.Implementation;
using HealthIntelligence.Common.Interface;
using HealthIntelligence.Common.Models;
using HealthIntelligence.Data;
using HealthIntelligence.Dtos.chatDto;
using HealthIntelligence.Dtos.CommonDto;
using HealthIntelligence.Dtos.PublicDto;
using HealthIntelligence.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace HealthIntelligence.Services
{
    [AllowAnonymous]
    public class PublicService : IPublicService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAppLogger _appLogger;
        private readonly IWebHostEnvironment _env;
        private readonly IMemoryCache _cache;
        private readonly ICommonService _commonService;
        private readonly IAIAnalyzeService _aIAnalyzeService;

        public PublicService(ApplicationDbContext context, IAppLogger appLogger, IWebHostEnvironment env, 
            IMemoryCache cache, ICommonService commonService, IAIAnalyzeService aIAnalyzeService)
        {
            _context = context;
            _appLogger = appLogger;
            _env = env;
            _cache = cache;
            _commonService = commonService;
            _aIAnalyzeService = aIAnalyzeService;
        }
        public async Task<ResultResponseDto<List<PartnerCountryResponseDto>>> GetAllCountries()
        {
            try
            {
                var result = await _context.Countries.Where(c => c.IsActive && !c.IsDeleted).
                 Select(c => new PartnerCountryResponseDto
                 {
                     CountryID = c.CountryID,
                     Continent = c.Continent,
                     CountryName = c.CountryName,
                     CountryCode = c.CountryCode,
                     Region = c.Region,
                     Country = c.CountryName
                 }).OrderBy(x => x.CountryName).ToListAsync();

                return ResultResponseDto<List<PartnerCountryResponseDto>>.Success(result, new string[] { "get All Countries successfully" });
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occure in getAllCountries", ex);
                return ResultResponseDto<List<PartnerCountryResponseDto>>.Failure(new string[] { "There is an error please try later" });
            }
        }
        public async Task<ResultResponseDto<PartnerCountryFilterResponse>> GetPartnerCountriesFilterRecord()
        {
            try
            {
                // Fetch all active countries once
                var activeCountries = await _context.Countries
                    .Where(x => !x.IsDeleted)
                    .ToListAsync();

                var res = new PartnerCountryFilterResponse
                {
                    //Countries = activeCountries
                    //    .Select(x => new
                    //    {
                    //        x.CountryID,
                    //        x.CountryName
                    //    })
                    //    .Distinct()
                    //    .ToList(),

                    Countries = activeCountries
                        .Select(x => new PartnerCountryDto
                        {
                            CountryID = x.CountryID,
                            CountryName = x.CountryName
                        })
                        .ToList(),

                    Regions = activeCountries
                        .Select(x => x.Region)
                        .Where(r => !string.IsNullOrEmpty(r))
                        .Distinct()
                        .ToList()
                };

                return ResultResponseDto<PartnerCountryFilterResponse>.Success(
                    res,
                    new List<string> { "Get Countries history successfully" }
                );
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occured in GetPartnerCountriesFilterRecord", ex);
                return ResultResponseDto<PartnerCountryFilterResponse>.Failure(
                    new string[] { "Failed to get Partner Country filter data" }
                );
            }
        }

        public async Task<ResultResponseDto<List<PillarResponseDto>>> GetAllPillarAsync()
        {
            try
            {
                var res =  await _context.Pillars
                .OrderBy(p => p.DisplayOrder)
                .Select(x => new PillarResponseDto
                {
                    DisplayOrder = x.DisplayOrder,
                    PillarID = x.PillarID,
                    PillarName = x.PillarName,
                    ImagePath = x.ImagePath
                }).ToListAsync();
                return ResultResponseDto<List<PillarResponseDto>>.Success(res, new List<string> { "Get Countries history successfully" });

            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occure in GetAllPillarAsync", ex);
                return ResultResponseDto<List<PillarResponseDto>>.Failure(new string[] { "Failed to get Piilar detail" });
            }
        }
        public async Task<PaginationResponse<PartnerCountryResponseDto>> GetPartnerCountries(PartnerCountryRequestDto request)
        {
            try
            {
                var year = DateTime.Now.Year;


                var countryQuery =
                   from c in _context.Countries.Where(x => !request.CountryID.HasValue || x.CountryID == request.CountryID)
                   join uc in _context.UserCountryMappings on c.CountryID equals uc.CountryID into ucg
                   from uc in ucg.DefaultIfEmpty()
                   join a in _context.Assessments on uc.UserCountryMappingID equals a.UserCountryMappingID into ag
                   from a in ag.DefaultIfEmpty()
                   join pa in _context.PillarAssessments.Where(x=> !request.PillarID.HasValue || x.PillarID == request.PillarID) 
                   on a.AssessmentID equals pa.AssessmentID into pag
                   from pa in pag.DefaultIfEmpty()
                   join r in _context.AssessmentResponses on pa.PillarAssessmentID equals r.PillarAssessmentID into rg
                   from r in rg.DefaultIfEmpty()
                   where !c.IsDeleted && 
                    (uc == null || !uc.IsDeleted) &&
                    (a == null || a.UpdatedAt.Year == year) 
                   group r by new
                   {
                       c.CountryID,
                       c.CountryCode,
                       c.Image,
                       c.Continent,
                       c.CountryName,
                       c.Region,
                       EvaluatorCount = _context.UserCountryMappings
                                           .Count(x => x.CountryID == c.CountryID && !x.IsDeleted)
                   }
                   into g
                   select new PartnerCountryResponseDto
                   {
                       CountryID = g.Key.CountryID,
                       Continent = g.Key.Continent,
                       CountryName = g.Key.CountryName,
                       CountryCode = g.Key.CountryCode,
                       Region = g.Key.Region,                       
                       Image = g.Key.Image,
                       Score = (decimal)g.Sum(x => (int?)x.Score ?? 0) / (g.Key.EvaluatorCount == 0 ? 1 : g.Key.EvaluatorCount),
                       HighScore = g.Max(x=>(int?)x.Score ?? 0),
                       LowerScore = g.Min(x => (int?)x.Score ?? 0),
                       Progress = ((decimal)g.Sum(x => (int?)x.Score ?? 0) * 100) / ((g.Key.EvaluatorCount == 0 ? 1 : g.Key.EvaluatorCount) * 4 * g.Count()),
                   };

                if (!string.IsNullOrWhiteSpace(request.Country))
                {
                    countryQuery = countryQuery.Where(c => c.Country.Contains(request.Country));
                }

                // Only filter by Region if a value is provided
                if (!string.IsNullOrWhiteSpace(request.Region))
                {
                    countryQuery = countryQuery.Where(c => c.Region != null && c.Region.Contains(request.Region));
                }

                var response = await countryQuery.ApplyPaginationAsync(request);

                return response;

            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occure in GetCountriesProgressByUserId", ex);
                return new();
            }
        }

        public async Task<CountryCityResponse> GetCountriesAndCities_WithStaleSupport()
        {
            try
            {
                string jsonFilePath = Path.Combine(_env.WebRootPath, "data\\countries_cache.json");
                if (!File.Exists(jsonFilePath))
                    return new CountryCityResponse(); // ✅ NEVER return null

                var json = await File.ReadAllTextAsync(jsonFilePath);

                var data = JsonSerializer.Deserialize<CountryCityResponse>(json);

                return data ?? new CountryCityResponse();
            }
            catch (Exception ex)
            {
                // ✅ Optional: log error
                // _logger.LogError(ex, "Failed to load country-city file");

                return new CountryCityResponse(); // ✅ Safe fallback
            }
        }

        public async Task<ResultResponseDto<List<PromotedPillarsResponseDto>>> GetPromotedCountries()
        {
            const string cacheKey = "PromotedCities";

            try
            {
                // ✅ Try get from cache
                if (_cache.TryGetValue(cacheKey, out List<PromotedPillarsResponseDto> cachedData))
                {
                    return ResultResponseDto<List<PromotedPillarsResponseDto>>.Success(
                        cachedData,
                        new List<string> { "Promoted countries fetched successfully" }
                    );
                }

                int currentYear = DateTime.Now.Year;

                var admin = await _context.Users.FirstOrDefaultAsync(x => x.Role == Models.UserRole.Admin);

                int role = (int)(admin?.Role ?? Models.UserRole.Admin);

                var pillarScores = await _commonService.GetCountriesProgressAsync(admin?.UserID ?? 0, role, currentYear);


                var result = await _context.AIPillarScores
                    .Include(x => x.Country)
                    .Include(x => x.Pillar)
                    .Where(x =>
                        x.Year == currentYear &&
                        x.Country.IsActive &&
                        !x.Country.IsDeleted)
                    .GroupBy(x => new
                    {
                        x.PillarID,
                        x.Pillar.PillarName,
                        x.Pillar.DisplayOrder,
                        x.Pillar.ImagePath
                    })
                    .Select(g => new PromotedPillarsResponseDto
                    {
                        PillarID = g.Key.PillarID,
                        PillarName = g.Key.PillarName,
                        DisplayOrder = g.Key.DisplayOrder,
                        ImagePath = g.Key.ImagePath,
                        Countries = g
                            .OrderByDescending(x => x.AIProgress)
                            .Take(3)
                            .Select(c => new PromotedCountryResponseDto
                            {
                                CountryID = c.CountryID,
                                CountryName = c.Country.CountryName,                                
                                Continent = c.Country.Continent,
                                CountryCode = c.Country.CountryCode,
                                Region = c.Country.Region,
                                Image = c.Country.Image,
                                ScoreProgress = c.AIProgress,
                                Description = c.EvidenceSummary,
                            }).ToList()
                    })
                    .OrderBy(p => p.DisplayOrder)
                    .ToListAsync();

                foreach (var pillar in result)
                {
                    foreach (var country in pillar.Countries)
                    {
                        var score = pillarScores
                            .Where(s => s.CountryID == country.CountryID && s.PillarID == pillar.PillarID)
                            .Select(s => s.ScoreProgress)
                            .FirstOrDefault();
                        country.ScoreProgress = score;
                    }
                    pillar.Countries = pillar.Countries.OrderByDescending(c => c.ScoreProgress).ToList();
                }

                _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                    SlidingExpiration = TimeSpan.FromMinutes(5),
                    Priority = CacheItemPriority.High
                });

                return ResultResponseDto<List<PromotedPillarsResponseDto>>.Success(
                    result,
                    new List<string> { "Promoted countries fetched successfully" }
                );
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occurred in GetPromotedCountries", ex);
                return ResultResponseDto<List<PromotedPillarsResponseDto>>.Failure(
                    new[] { "Failed to get promoted countries" }
                );
            }
        }

        public async Task<ResultResponseDto<EmergingTrendsResult>> GetEmergingTrendsAndIssues(int countryCount)
        {
            try
            {
                var cacheKey = EmergingTrendsCacheKey(countryCount);

                if (_cache.TryGetValue(cacheKey, out EmergingTrendsResult cachedResult)
                    && cachedResult?.Countries?.Count > 0)
                {
                    return ResultResponseDto<EmergingTrendsResult>.Success(
                        cachedResult,
                        new List<string>
                        {
                            "Emerging trends and issues fetched successfully from cache."
                        }
                    );
                }

                return ResultResponseDto<EmergingTrendsResult>.Failure(
                    new[]
                    {
                        "Emerging trends feed is being updated. Please try again shortly."
                    }
                );
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync(
                    "An error occurred while processing the GetEmergingTrendsAndIssues request.",
                    ex
                );

                return ResultResponseDto<EmergingTrendsResult>.Failure(
                    new[]
                    {
                        "An error occurred while processing your request. Please try again later."
                    }
                );
            }
        }

        public async Task<ResultResponseDto<PillarLiveSignalsResult>> GetPillarLiveSignals()
        {
            const string cacheKey = "PillarLiveSignals";

            try
            {
                if (_cache.TryGetValue(cacheKey, out PillarLiveSignalsResult cachedResult))
                {
                    return ResultResponseDto<PillarLiveSignalsResult>.Success(
                        cachedResult,
                        new List<string>
                        {
                            "Pillar live signals fetched successfully from cache."
                        }
                    );
                }

                var result = await _aIAnalyzeService.GetPillarLiveSignals();

                if (result == null || result.Success != true)
                {
                    return ResultResponseDto<PillarLiveSignalsResult>.Failure(
                        new[]
                        {
                            result?.Message ??
                            "Failed to fetch pillar live signals."
                        }
                    );
                }

                var pillarLookup = await _context.Pillars
                    .AsNoTracking()
                    .Select(p => new
                    {
                        p.PillarID,
                        p.PillarName,
                        p.ImagePath,
                        p.DisplayOrder
                    })
                    .ToListAsync();

                foreach (var pillarCard in result.Result.Pillars)
                {
                    var matched = pillarLookup.FirstOrDefault(p => p.PillarID == pillarCard.PillarId);
                    pillarCard.PillarName = matched?.PillarName ?? $"Pillar {pillarCard.PillarId}";
                    pillarCard.ImagePath = matched?.ImagePath ?? "";
                }

                result.Result.Pillars = result.Result.Pillars
                    .OrderBy(p =>
                    {
                        var order = pillarLookup.FirstOrDefault(x => x.PillarID == p.PillarId)?.DisplayOrder;
                        return order ?? p.PillarId;
                    })
                    .ToList();

                _cache.Set(
                    cacheKey,
                    result.Result,
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12),
                        SlidingExpiration = TimeSpan.FromHours(10),
                        Priority = CacheItemPriority.High
                    }
                );

                return ResultResponseDto<PillarLiveSignalsResult>.Success(
                    result.Result,
                    new List<string>
                    {
                        "Pillar live signals fetched successfully."
                    }
                );
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync(
                    "An error occurred while processing the GetPillarLiveSignals request.",
                    ex
                );

                return ResultResponseDto<PillarLiveSignalsResult>.Failure(
                    new[]
                    {
                        "An error occurred while processing your request. Please try again later."
                    }
                );
            }
        }

        public async Task<bool> RefreshEmergingTrendsCacheAsync(
            int countryCount,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var enriched = await FetchAndEnrichEmergingTrendsAsync(countryCount, cancellationToken);

                if (enriched == null || enriched.Countries == null || enriched.Countries.Count == 0)
                {
                    return false;
                }

                var cacheKey = EmergingTrendsCacheKey(countryCount);
                _cache.Set(
                    cacheKey,
                    enriched,
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12),
                        Priority = CacheItemPriority.High
                    }
                );

                return true;
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync(
                    "An error occurred while refreshing the emerging trends cache.",
                    ex
                );
                return false;
            }
        }
        private static string EmergingTrendsCacheKey(int countryCount) =>
           $"EmergingTrendsAndIssues_{countryCount }";

        private async Task<EmergingTrendsResult?> FetchAndEnrichEmergingTrendsAsync(
            int countryCount,
            CancellationToken cancellationToken = default)
        {
            var result = await _aIAnalyzeService.GetEmergingTrendsAndIssues(countryCount);

            if (result == null || result.Success != true || result.Result == null)
            {
                return null;
            }

            if (result.Result.Countries == null || result.Result.Countries.Count == 0)
            {
                return null;
            }            

            return result.Result;
        }

    }
}

public class CountryCityResponse
{
    public bool error { get; set; }
    public string msg { get; set; }
    public List<CountryData> data { get; set; }
}

public class CountryData
{
    public string country { get; set; }
    public List<string> countries { get; set; }
}


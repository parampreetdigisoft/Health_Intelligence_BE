using HealthIntelligence.Backgroundjob;
using HealthIntelligence.Common.Implementation;
using HealthIntelligence.Common.Interface;
using HealthIntelligence.Common.Models;
using HealthIntelligence.Common.Models.settings;
using HealthIntelligence.Data;
using HealthIntelligence.Dtos.AiDto;
using HealthIntelligence.Dtos.CountryDto;
using HealthIntelligence.Dtos.CommonDto;
using HealthIntelligence.IServices;
using HealthIntelligence.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Linq.Expressions;

namespace HealthIntelligence.Services
{
    public class AIComputationService : IAIComputationService
    {
        #region constructor
        
        private readonly ApplicationDbContext _context;
        private readonly IAppLogger _appLogger;
        private readonly ICommonService _commonService;
        private readonly Download _download;
        private readonly IAIAnalyzeService _iAIAnalayzeService;
        private readonly IDocumentGeneratorService _documentGeneratorService;
        private readonly IWebHostEnvironment _env;
        private readonly AppSettings _appSettings;
        public AIComputationService(ApplicationDbContext context, IAppLogger appLogger, ICommonService commonService,
            Download download, IAIAnalyzeService iAIAnalayzeService, IDocumentGeneratorService documentGeneratorService,
            IWebHostEnvironment env, IOptions<AppSettings> appSettings)
        {
            _context = context;
            _appLogger = appLogger;
            _commonService = commonService;
            _download = download;
            _iAIAnalayzeService = iAIAnalayzeService;
            _documentGeneratorService = documentGeneratorService;
            _env = env;
            _appSettings = appSettings.Value;
        }
        #endregion

        #region implementation


        public async Task<ResultResponseDto<List<AITrustLevel>>> GetAITrustLevels()
        {
            var r = await _context.AITrustLevels.ToListAsync();

            return ResultResponseDto<List<AITrustLevel>>.Success(r, new[] { "Pillar get successfully" });

        }
        public async Task<PaginationResponse<AiCountrySummeryDto>> GetAICountries(AiCountrySummeryRequestDto request, int userID, UserRole userRole)
        {
            try
            {
                IQueryable<AiCountrySummeryDto> query = await GetCountryAiSummeryDetails(userID, userRole, request.CountryID, request.Year);

                var result = await query.ApplyPaginationAsync(request);
                var analyticalLayers = _context.AnalyticalLayers.AsQueryable();
                int pillarCount = _appSettings.PillarCount;
                var totalValidKpis = await analyticalLayers.Distinct().CountAsync();

                foreach (var c in result.Data)
                {
                    c.EvidenceSummary = CommonService.InitailLineOfExecutiveSummery(c.EvidenceSummary, c.ImmediateSituationSummary, c.AIProgress, c.CountryName, pillarCount, totalValidKpis);
                }

                if (userRole != UserRole.CountryUser)
                {
                    var progress = await _commonService.GetCountriesProgressAsync(userID, (int)userRole, request.Year);

                    var ids = result.Data.Select(x => x.CountryID);
                    var countries = progress.Where(x => ids.Contains(x.CountryID));


                    var counts = await _context.Pillars
                        .Select(p => p.Questions.Count()).ToListAsync();

                    var totalQuestions = counts.Sum();

                    var answeredQuestions = await _context.AIEstimatedQuestionScores
                        .Where(x => x.Year == request.Year && ids.Contains(x.CountryID))
                        .GroupBy(x => x.CountryID)
                        .Select(g => new
                        {
                            CountryID = g.Key,
                            CompletionRate = totalQuestions == 0
                                ? 0
                                : g.Count() * 100.0M / totalQuestions
                        })
                        .ToListAsync();


                    foreach (var c in result.Data)
                    {
                        var pillars = countries.Where(x => x.CountryID == c.CountryID);

                        var countryScore = Math.Round(pillars.Sum(x => x.ScoreProgress) / _appSettings.PillarCount,2);

                        c.EvaluatorProgress = countryScore;
                        c.Discrepancy = Math.Abs(countryScore - (c.AIProgress ?? 0));
                        c.AICompletionRate = answeredQuestions.FirstOrDefault(x=>x.CountryID== c.CountryID)?.CompletionRate;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occured in GetCountriesAsync", ex);
                return new PaginationResponse<AiCountrySummeryDto>();
            }
        }
        public async Task<IQueryable<AiCountrySummeryDto>> GetCountryAiSummeryDetails(int userID, UserRole userRole, int? countryID, int currentYear=0)
        {
            currentYear = currentYear ==0 ? DateTime.Now.Year : currentYear;
            var firstDate = new DateTime(currentYear, 1, 1); 
            var endDate = new DateTime(currentYear+1, 1, 1); 
            IQueryable<AICountryScore> baseQuery = _context.AICountryScores.Where(x=> x.UpdatedAt >= firstDate && x.UpdatedAt < endDate && x.Year== currentYear && x.Country.IsActive && !x.Country.IsDeleted);

            List<int> allowedCountryIDs = new();
            if (userRole == UserRole.Analyst)
            {
                // Allowed country IDs
                 allowedCountryIDs = await _context.UserCountryMappings
                            .Where(x => !x.IsDeleted && x.UserID == userID && (!countryID.HasValue || x.CountryID == countryID.Value))
                            .Select(x => x.CountryID)
                            .Distinct()
                            .ToListAsync();

                baseQuery = baseQuery.Where(x => allowedCountryIDs.Contains(x.CountryID));
            }
            else if (userRole == UserRole.Evaluator)
            {
                // Allowed country IDs
                 allowedCountryIDs = await _context.AIUserCountryMappings
                            .Where(x => x.IsActive && x.UserID == userID && (!countryID.HasValue || x.CountryID == countryID.Value))
                            .Select(x => x.CountryID)
                            .Distinct()
                            .ToListAsync();

                baseQuery = baseQuery.Where(x => allowedCountryIDs.Contains(x.CountryID));
            }
            else if (userRole == UserRole.CountryUser)
            {
                 allowedCountryIDs = await _context.PublicUserCountryMappings
                            .Where(x => x.IsActive && x.UserID == userID && (!countryID.HasValue || x.CountryID == countryID.Value))
                            .Select(x => x.CountryID)
                            .Distinct()
                            .ToListAsync();

                baseQuery = baseQuery.Where(x => allowedCountryIDs.Contains(x.CountryID) && x.IsVerified);
            }
            else
            {
                // Admin
                if (countryID.HasValue)
                {
                    baseQuery = baseQuery.Where(x => x.CountryID == countryID.Value);
                    allowedCountryIDs = new() { countryID.Value };
                }
            }
            var commentQuery = _context.AIUserCountryMappings
                .Where(x =>
                    (
                        userRole == UserRole.Admin ||
                        (userRole == UserRole.Analyst && x.AssignBy == userID) ||
                        (userRole == UserRole.Evaluator && x.UserID == userID)
                    )
                )
                .GroupBy(x => x.CountryID)
                .Select(g => new
                {
                    CountryID = g.Key,
                    Comment = g
                        .OrderByDescending(x => x.UpdatedAt >= firstDate && x.UpdatedAt < endDate)
                        .Select(x => x.Comment)
                        .FirstOrDefault()
                });

            var query =
                from c in _context.Countries
                where allowedCountryIDs.Contains(c.CountryID) || (userRole == UserRole.Admin && !countryID.HasValue) && c.IsActive && !c.IsDeleted

                join score in baseQuery
                    on c.CountryID equals score.CountryID
                    into scoreJoin
                from score in scoreJoin.DefaultIfEmpty()   // LEFT JOIN score

                join cmt in commentQuery
                    on c.CountryID equals cmt.CountryID
                    into cmtJoin
                from cmt in cmtJoin.DefaultIfEmpty()       // LEFT JOIN comment

                select new AiCountrySummeryDto
                {
                    CountryID = c.CountryID,
                    Continent = c.Continent ?? string.Empty,
                    CountryName = c.CountryName ?? string.Empty,                    
                    Image = c.Image ?? string.Empty,
                    Region = c.Region ?? string.Empty,
                    ScoringYear = score != null ? score.Year : currentYear,
                    AIScore = score != null ? score.AIScore : 0,
                    AIProgress = score != null ? score.AIProgress : null,
                    EvaluatorProgress = score != null ? score.EvaluatorProgress : null,
                    Discrepancy = score != null ? score.Discrepancy : null,
                    ConfidenceLevel = score != null ? score.ConfidenceLevel : string.Empty,

                    EvidenceSummary = score != null ? score.EvidenceSummary : string.Empty,
                    CrossPillarPatterns = score != null ? score.CrossPillarPatterns : string.Empty,
                    InstitutionalCapacity = score != null ? score.InstitutionalCapacity : string.Empty,
                    EquityAssessment = score != null ? score.EquityAssessment : string.Empty,
                    SustainabilityOutlook = score != null ? score.SustainabilityOutlook : string.Empty,
                    StrategicRecommendations = score != null ? score.StrategicRecommendations : string.Empty,
                    DataTransparencyNote = score != null ? score.DataTransparencyNote : string.Empty,

                    UpdatedAt = score != null ? score.UpdatedAt : null,
                    IsVerified = score != null ? score.IsVerified : false,

                    Comment = cmt != null ? cmt.Comment : null,
                    ImmediateSituationSummary = score != null ? score.ImmediateSituationSummary : null,
                    KeyDevelopments = score != null ? score.KeyDevelopments : null,
                    CriticalRisks = score != null ? score.CriticalRisks : null,
                    Gaps = score != null ? score.Gaps : null
                };



            return query;
        }
    
        public async Task<ResultResponseDto<AiCountryPillarReponseDto>> GetAICountryPillars(int countryID, int userID, UserRole userRole, int currentYear = 0)
        {
            try
            {
                currentYear = currentYear == 0 ? DateTime.Now.Year : currentYear;
                var firstDate = new DateTime(currentYear, 1, 1);

                var res = await _context.AIPillarScores
                    .Where(x => x.CountryID == countryID && x.UpdatedAt >= firstDate && x.Year == currentYear)
                    .Include(x=>x.Country)
                    .Include(x => x.DataSourceCitations)
                    .ToListAsync();

                List<int> pillarIds = new();
                if (userRole == UserRole.CountryUser)
                {
                    pillarIds = await _context.CountryUserPillarMappings
                                .Where(x => x.IsActive && x.UserID == userID)
                                .Select(x => x.PillarID)
                                .Distinct()
                                .ToListAsync();
                }
                var pillars = await _context.Pillars.Select(x=>new
                {
                    PillarID = x.PillarID,
                    PillarName = x.PillarName,
                    DisplayOrder = x.DisplayOrder,
                    ImagePath = x.ImagePath,
                    TotalQuestions = x.Questions.Count()
                }).ToListAsync();

                var result = pillars
                .GroupJoin(
                    res,
                    p => p.PillarID,
                    s => s.PillarID,
                    (pillar, scores) => new { pillar, score = scores.FirstOrDefault() }
                )
                .Select(x =>
                {
                    var isAccess = pillarIds.Count == 0 || pillarIds.Contains(x.pillar.PillarID);

                    var r = new AiCountryPillarReponse
                    {
                        PillarScoreID = x.score?.PillarScoreID ?? 0,
                        CountryID = x.score?.CountryID ?? countryID,
                        CountryName = x.score?.Country?.CountryName ?? "",
                        Continent = x.score?.Country?.Continent ?? "",
                        Region = x.score?.Country?.Region ?? "",
                        PillarID = x.pillar.PillarID,
                        PillarName = x.pillar.PillarName,
                        DisplayOrder = x.pillar.DisplayOrder,
                        ImagePath = x.pillar.ImagePath,
                        IsAccess = isAccess
                    };

                    if (isAccess && x.score != null)
                    {
                        r.AIDataYear = x.score.Year;
                        r.AIScore = x.score.AIScore;
                        r.AIProgress = x.score.AIProgress;
                        r.EvaluatorProgress = x.score.EvaluatorProgress;
                        r.Discrepancy = x.score.Discrepancy;
                        r.ConfidenceLevel = x.score.ConfidenceLevel;
                        r.EvidenceSummary = x.score.EvidenceSummary;
                        r.RedFlags = x.score.RedFlags;
                        r.GeographicEquityNote = x.score.GeographicEquityNote;
                        r.InstitutionalAssessment = x.score.InstitutionalAssessment;
                        r.DataGapAnalysis = x.score.DataGapAnalysis;
                        r.AnalystDataGapAnalysis = x.score.AnalystDataGapAnalysis ?? string.Empty;
                        r.DataSourceCitations = x.score.DataSourceCitations;
                        r.UpdatedAt = x.score.UpdatedAt;
                    }
                    return r;
                })
                .OrderBy(x => !x.IsAccess)
                .ThenBy(x => x.DisplayOrder)
                .ToList();

                var progress = await _commonService.GetCountriesProgressAsync(userID, (int)userRole, currentYear);

                var countries = progress.Where(x => x.CountryID == countryID);

                var answeredQuestions = await _context.AIEstimatedQuestionScores
               .Where(x => x.Year == currentYear && x.CountryID == countryID)
               .GroupBy(x => x.PillarID)
               .Select(g => new
               {
                   PillarID = g.Key,
                   AnsweredQuestions = g.Count() 
               })
               .ToListAsync();

                foreach (var c in result)
                {
                    var totalQuestions = pillars.FirstOrDefault(x => x.PillarID == c.PillarID)?.TotalQuestions ?? 1;
                    var answeredQuestion = answeredQuestions.FirstOrDefault(x => x.PillarID == c.PillarID)?.AnsweredQuestions ?? 0;

                    var pillarScore = countries
                        .Where(x => x.PillarID == c.PillarID)
                        .Select(x => x.ScoreProgress)
                        .DefaultIfEmpty(0)
                        .Sum();


                    c.EvaluatorProgress = pillarScore;
                    c.Discrepancy = Math.Abs(pillarScore - (c.AIProgress ?? 0));
                    c.AICompletionRate = answeredQuestion * 100.0M / totalQuestions;
                }

                var finalResutl = new AiCountryPillarReponseDto
                {
                    Pillars = result
                };

                var resposne = ResultResponseDto<AiCountryPillarReponseDto>.Success(finalResutl, new[] { "Pillar get successfully", });

                return resposne;
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occured in GetAICountryPillars", ex);
                return ResultResponseDto<AiCountryPillarReponseDto>.Failure(new[] { "Error in getting pillar details", });
            }
        }
        public async Task<PaginationResponse<AIEstimatedQuestionScoreDto>> GetAIPillarsQuestion(AiCountryPillarSummeryRequestDto request, int userID, UserRole userRole)
        {
            try
            {
                if (userRole == UserRole.CountryUser && request.CountryID != null && request.PillarID != null)
                {
                    var isPillarAccess = _context.CountryUserPillarMappings
                                .Where(x => x.IsActive && x.UserID == userID)
                                .Select(x => x.PillarID).Contains(request.PillarID.Value);

                    var isCountryAccess = _context.PublicUserCountryMappings
                               .Where(x => x.IsActive && x.UserID == userID)
                               .Select(x => x.CountryID).Contains(request.CountryID.Value);
                    if (!(isCountryAccess && isPillarAccess))
                    {
                        return new PaginationResponse<AIEstimatedQuestionScoreDto>();
                    }
                }
                var currentYear = request.Year;
                var firstDate = new DateTime(currentYear, 1, 1);

                var res =
                    from q in _context.Questions.Where(x=>x.PillarID== request.PillarID)
                    join s in _context.AIEstimatedQuestionScores
                        .Where(x =>
                            x.CountryID == request.CountryID &&
                            x.PillarID == request.PillarID &&
                            x.UpdatedAt >= firstDate && x.Year == currentYear)
                    on q.QuestionID equals s.QuestionID into qs
                    from x in qs.DefaultIfEmpty() // LEFT JOIN
                    select new AIEstimatedQuestionScoreDto
                    {
                        CountryID = x == null ? request.CountryID ??0  : x.CountryID,
                        PillarID = x == null ? request.PillarID ?? 0 : x.PillarID,
                        QuestionID = q.QuestionID,
                        DataYear = x == null ? currentYear : x.Year,
                        AIScore = x == null ? null : x.AIScore,
                        AIProgress = x == null ? null : x.AIProgress,
                        EvaluatorProgress = x == null ? null : x.EvaluatorProgress,
                        Discrepancy = x == null ? null : x.Discrepancy,
                        ConfidenceLevel = x == null ? string.Empty : x.ConfidenceLevel,
                        DataSourcesUsed = x == null ? null : x.DataSourcesUsed,
                        EvidenceSummary = x == null ? string.Empty : x.EvidenceSummary,
                        RedFlags = x == null ? string.Empty : x.RedFlags,
                        GeographicEquityNote = x == null ? string.Empty : x.GeographicEquityNote,
                        SourceType = x == null ? string.Empty : x.SourceType,
                        SourceName = x == null ? string.Empty : x.SourceName,
                        SourceURL = x == null ? string.Empty : x.SourceURL,
                        SourceDataExtract = x == null ? string.Empty : x.SourceDataExtract,
                        SourceDataYear = x == null ? null : x.SourceDataYear,
                        SourceTrustLevel = x == null ? null : x.SourceTrustLevel,
                        UpdatedAt = x == null ? null : x.UpdatedAt,
                        QuestionText = q.QuestionText == null ? string.Empty : q.QuestionText
                    };

                var r = await res.ApplyPaginationAsync(request);

                return r;
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occured in GetAICountryPillars", ex);
                return new PaginationResponse<AIEstimatedQuestionScoreDto>();
            }
        }
        public async Task<ResultResponseDto<AiCrossCountryResponseDto>> GetAICrossCountryPillars(AiCountryIdsDto countryIds, int userID, UserRole userRole)
        {
            try
            {
                var currentYear = DateTime.Now.Year;
                var response = new AiCrossCountryResponseDto();

                var firstDate = new DateTime(currentYear, 1, 1);

                var aiPillarScores = await _context.AIPillarScores
                    .Where(x => countryIds.CountryIDs.Contains(x.CountryID) && x.UpdatedAt >= firstDate)
                    .ToListAsync();

                var countries = await _context.Countries
                    .Where(x => countryIds.CountryIDs.Contains(x.CountryID))
                    .ToListAsync();

                // Pillar access based on role
                List<int> pillarIds = new();
                if (userRole == UserRole.CountryUser)
                {
                    pillarIds = await _context.CountryUserPillarMappings
                        .Where(x => x.IsActive && x.UserID == userID)
                        .Select(x => x.PillarID)
                        .Distinct()
                        .ToListAsync();
                }

                var pillars = await _context.Pillars.ToListAsync();

                // Categories
                response.Categories.AddRange(
                    pillars
                        .Where(x => pillarIds.Count == 0 || pillarIds.Contains(x.PillarID))
                        .OrderBy(x=>x.DisplayOrder)
                        .Select(x => x.PillarName)
                );
                // Per country processing

                var aiCountries = await _context.AICountryScores
                    .Where(x => countryIds.CountryIDs.Contains(x.CountryID) &&
                                x.Year == currentYear && ((userRole == UserRole.CountryUser && x.IsVerified) || userRole != UserRole.CountryUser))
                    .GroupBy(x => x.CountryID)
                    .Select(g => new
                    {
                        CountryID = g.Key,
                        UpdatedAt = g.Max(x => x.UpdatedAt),
                        AIProgress = g.Max(x => x.AIProgress)
                    })
                    .ToDictionaryAsync(x => x.CountryID, x => new { x.AIProgress, x.UpdatedAt });


                foreach (var country in countries)
                {
                    var pillarResults = pillars
                    .GroupJoin(
                        aiPillarScores.Where(x => x.CountryID == country.CountryID),
                        p => p.PillarID,
                        s => s.PillarID,
                        (pillar, scores) => new
                        {
                            Pillar = pillar,
                            Score = scores.FirstOrDefault()
                        })
                    .Select(x =>
                    {
                        var isAccess = pillarIds.Count == 0 || pillarIds.Contains(x.Pillar.PillarID);

                        return new CrossCountryPillarValueDto
                        {
                            PillarID = x.Pillar.PillarID,
                            PillarName = x.Pillar.PillarName,
                            Value = isAccess ? x.Score?.AIProgress ?? 0 : 0,
                            IsAccess = isAccess,
                            DisplayOrder = x.Pillar.DisplayOrder
                        };
                    })
                    .OrderBy(x => !x.IsAccess)
                    .ThenBy(x => x.DisplayOrder)
                    .ToList();
                    var chartRow = new CrossCountryChartTableRowDto
                    {
                        CountryID = country.CountryID,
                        CountryName = country.CountryName,
                        PillarValues = pillarResults.ToList()
                    };
                    if (aiCountries?.TryGetValue(country.CountryID,out var aiCountryValue) ?? false)
                    {
                        chartRow.Value = aiCountryValue.AIProgress ?? 0;
                        chartRow.UpdatedAt = aiCountryValue.UpdatedAt;
                    }
                    response.TableData.Add(chartRow);

                    var series = new CrossCountryChartSeriesDto
                    {
                        Name = country.CountryName,
                        Data = pillarResults
                            .Where(x => x.IsAccess)
                            .Select(x => x.Value).ToList()
                    };
                    response.Series.Add(series);
                }

                return ResultResponseDto<AiCrossCountryResponseDto>.Success(response,new[] { "Pillars fetched successfully" });
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error occurred in GetAICrossCountryPillars", ex);
                return ResultResponseDto<AiCrossCountryResponseDto>.Failure(new[] { "Error in getting pillar details" });
            }
        }
        public async Task<ResultResponseDto<Dictionary<int, List<AiCountryPillarReponse>>>> GetAllCountriesAIPillars(
         int userID, UserRole userRole, int currentYear = 0)
        {
            try
            {
                currentYear = currentYear == 0 ? DateTime.Now.Year : currentYear;
                var firstDate = new DateTime(currentYear, 1, 1);

                var scores = await _context.AIPillarScores
                    .Where(x => x.UpdatedAt >= firstDate && x.Year == currentYear)
                    .Include(x => x.Country)
                    .Include(x => x.DataSourceCitations)
                    .ToListAsync();

                List<int> pillarIds = new();
                if (userRole == UserRole.CountryUser)
                {
                    pillarIds = await _context.CountryUserPillarMappings
                        .Where(x => x.IsActive && x.UserID == userID)
                        .Select(x => x.PillarID)
                        .Distinct()
                        .ToListAsync();
                }

                var pillars = await _context.Pillars.Select(x => new
                {
                    x.PillarID,
                    x.PillarName,
                    x.DisplayOrder,
                    x.ImagePath,
                    TotalQuestions = x.Questions.Count()
                }).ToListAsync();

                var CountryIDs = scores.Select(x => x.CountryID).Distinct().ToList();

                var result = new Dictionary<int, List<AiCountryPillarReponse>>();

                foreach (var countryId in CountryIDs)
                {
                    var countryScores = scores.Where(x => x.CountryID == countryId).ToList();

                    var pillarResults = pillars
                        .GroupJoin(
                            countryScores,
                            p => p.PillarID,
                            s => s.PillarID,
                            (pillar, score) => new { pillar, score = score.FirstOrDefault() }
                        )
                        .Select(x =>
                        {
                            var isAccess = pillarIds.Count == 0 || pillarIds.Contains(x.pillar.PillarID);

                            var r = new AiCountryPillarReponse
                            {
                                PillarScoreID = x.score?.PillarScoreID ?? 0,
                                CountryID = x.score?.CountryID ?? countryId,
                                CountryName = x.score?.Country?.CountryName ?? "",
                                Continent = x.score?.Country?.Continent ?? "",
                                Region = x.score?.Country?.Region ?? "",
                                PillarID = x.pillar.PillarID,
                                PillarName = x.pillar.PillarName,
                                DisplayOrder = x.pillar.DisplayOrder,
                                ImagePath = x.pillar.ImagePath,
                                IsAccess = isAccess
                            };

                            if (isAccess && x.score != null)
                            {
                                r.AIDataYear = x.score.Year;
                                r.AIScore = x.score.AIScore;
                                r.AIProgress = x.score.AIProgress;
                                r.EvaluatorProgress = x.score.EvaluatorProgress;
                                r.Discrepancy = x.score.Discrepancy;
                                r.ConfidenceLevel = x.score.ConfidenceLevel;
                                r.EvidenceSummary = x.score.EvidenceSummary;
                                r.RedFlags = x.score.RedFlags;
                                r.GeographicEquityNote = x.score.GeographicEquityNote;
                                r.InstitutionalAssessment = x.score.InstitutionalAssessment;
                                r.DataGapAnalysis = x.score.DataGapAnalysis;
                                r.DataSourceCitations = x.score.DataSourceCitations;
                                r.UpdatedAt = x.score.UpdatedAt;
                            }

                            return r;
                        })
                        .OrderBy(x => !x.IsAccess)
                        .ThenBy(x => x.DisplayOrder)
                        .ToList();

                    result.Add(countryId, pillarResults);
                }

                var progress = await _commonService.GetCountriesProgressAsync(userID, (int)userRole, currentYear);

                var answeredQuestions = await _context.AIEstimatedQuestionScores
                    .Where(x => x.Year == currentYear)
                    .GroupBy(x => new { x.CountryID, x.PillarID })
                    .Select(g => new
                    {
                        g.Key.CountryID,
                        g.Key.PillarID,
                        AnsweredQuestions = g.Count()
                    })
                    .ToListAsync();

                foreach (var country in result)
                {
                    foreach (var c in country.Value)
                    {
                        var totalQuestions = pillars.FirstOrDefault(x => x.PillarID == c.PillarID)?.TotalQuestions ?? 1;

                        var answeredQuestion = answeredQuestions
                            .FirstOrDefault(x => x.CountryID == country.Key && x.PillarID == c.PillarID)?.AnsweredQuestions ?? 0;

                        var countryScore = progress
                            .Where(x => x.CountryID == country.Key && x.PillarID == c.PillarID)
                            .Select(x => x.ScoreProgress)
                            .DefaultIfEmpty(0)
                            .Sum();


                        c.EvaluatorProgress = countryScore;
                        c.Discrepancy = Math.Abs(countryScore - (c.AIProgress ?? 0));
                        c.AICompletionRate = answeredQuestion * 100.0M / totalQuestions;
                    }
                }

                var response = ResultResponseDto<Dictionary<int, List<AiCountryPillarReponse>>>
                    .Success(result, new[] { "All countries pillars fetched successfully" });

                return response;
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occured in GetAllCountriesAIPillars", ex);

                return ResultResponseDto<Dictionary<int, List<AiCountryPillarReponse>>>
                    .Failure(new[] { "Error in getting countries pillar details" });
            }
        }
        public async Task<ResultResponseDto<bool>> ChangedAiCountryEvaluationStatus(ChangedAiCountryEvaluationStatusDto dto, int userID, UserRole userRole)
        {
            try
            {
                var v = _context.UserCountryMappings.Any(x => x.UserID == userID && x.CountryID == dto.CountryID);
                if ((v && userRole == UserRole.Analyst) || userRole == UserRole.Admin)
                {

                    var aiResponse = await _context.AICountryScores.Where(x => x.CountryID == dto.CountryID && x.Year == DateTime.UtcNow.Year && x.Country.IsActive && !x.Country.IsDeleted).FirstOrDefaultAsync();
                    if (aiResponse != null)
                    {
                        aiResponse.IsVerified = dto.IsVerified;
                        aiResponse.VerifiedBy = userID;
                        
                        await _context.SaveChangesAsync();

                        _download.InsertAnalyticalLayerResults(dto.CountryID);
                        return ResultResponseDto<bool>.Success(true, new[] { dto.IsVerified ? "Finalize and lock the AI-generated score successfully" : "Reject the current AI-generated score Successfully" });
                    }
                }
                return ResultResponseDto<bool>.Failure(new[] { "Invalid country, please try again" });
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error in ChangedAiCountryEvaluationStatus", ex);
                return ResultResponseDto<bool>.Failure(new[] { "Error in Changed AiCountry Evaluation Status" });
            }
        }
        public async Task<ResultResponseDto<bool>> RegenerateAiSearch(RegenerateAiSearchDto dto,int userID, UserRole userRole)
        {
            try
            {
                if (dto.QuestionEnable)
                {
                    var currentYear = DateTime.Now.Year;

                    var aiQuestionList = await _context.AIEstimatedQuestionScores
                        .Where(x => x.CountryID == dto.CountryID && x.Year == currentYear)
                        .ToListAsync();

                    if (aiQuestionList.Count > 0)
                    {
                        _context.AIEstimatedQuestionScores.RemoveRange(aiQuestionList);
                        await _context.SaveChangesAsync();
                    }
                }


                await _download.AiResearchByCountryId(dto.CountryID, dto.CountryEnable, dto.PillarEnable, dto.QuestionEnable, dto.ImmediateSummaryEnable, dto.RegenerateMissingQuestionsEnable);
                var aiResponse = await _context.AICountryScores.FirstOrDefaultAsync(x => x.CountryID == dto.CountryID && x.Country.IsActive && !x.Country.IsDeleted);
                if(aiResponse != null)
                {
                    aiResponse.IsVerified = false;
                }
                // Assign viewers (optional)

                var aIUserCountryMappingsList = await _context.AIUserCountryMappings.Where(x => x.CountryID == dto.CountryID).ToListAsync();

                var um = _context.UserCountryMappings.Where(x => !x.IsDeleted && x.CountryID == dto.CountryID && dto.ViewerUserIDs.Contains(x.UserID));
                var valid = um.All(x => dto.ViewerUserIDs.Contains(x.UserID));

                string msg = "Evaluator not have access of this country please try again";

                if (dto.ViewerUserIDs != null && dto.ViewerUserIDs.Any() && valid)
                {
                    var existingMappings = aIUserCountryMappingsList.Where(x => dto.ViewerUserIDs.Contains(x.UserID));


                    var existingUserIds = existingMappings.Select(x => x.UserID).ToHashSet();

                    // Update existing mappings
                    foreach (var mapping in existingMappings)
                    {
                        mapping.IsActive = true;
                        mapping.UpdatedAt = DateTime.UtcNow;
                        mapping.AssignBy = userID;
                        mapping.Comment = string.Empty;
                    }

                    // Insert new mappings
                    var newMappings = dto.ViewerUserIDs
                        .Where(userId => !existingUserIds.Contains(userId))
                        .Select(userId => new AIUserCountryMapping
                        {
                            UserID = userId,
                            CountryID = dto.CountryID,
                            AssignBy = userID,
                            UpdatedAt = DateTime.UtcNow,
                            IsActive = true
                        });

                    await _context.AIUserCountryMappings.AddRangeAsync(newMappings);
                    msg = "Evaluator have access to view the country";
                }
                else if(aIUserCountryMappingsList.Count > 0)
                {
                    foreach (var mapping in aIUserCountryMappingsList)
                    {
                        mapping.IsActive = false;
                        mapping.UpdatedAt = DateTime.UtcNow;
                        mapping.AssignBy = userID;
                        mapping.Comment = string.Empty;
                    }
                }

                var msglist = new List<string>
                {
                    "AI research import has been initiated successfully"
                };

                if (dto.ViewerUserIDs != null && dto.ViewerUserIDs.Any())
                {
                    msglist.Add(msg);
                }
                await _context.SaveChangesAsync();
                return ResultResponseDto<bool>.Success(true, msglist);
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error in RegenerateAiSearch", ex);

                return ResultResponseDto<bool>.Failure(new[] { "Something went wrong while importing AI research. Please try again later." });
            }
        }
        public async Task<ResultResponseDto<bool>> AddComment(AddCommentDto dto, int userID, UserRole userRole)
        {
            try
            {
                var aIUserCountryMappings = await _context.AIUserCountryMappings.FirstOrDefaultAsync(x => x.UserID == userID && x.IsActive && x.CountryID == dto.CountryID);
                if (aIUserCountryMappings !=null && userRole == UserRole.Evaluator)
                {
                    aIUserCountryMappings.Comment = dto.Comment;

                    await _context.SaveChangesAsync();


                    await _context.SaveChangesAsync();
                    return ResultResponseDto<bool>.Success(true, new[] {"Comment Added Successfully"});

                }
                return ResultResponseDto<bool>.Failure(new[] { "Invalid country, please try again" });
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error in ChangedAiCountryEvaluationStatus", ex);
                return ResultResponseDto<bool>.Failure(new[] { "Error in Changed AiCountry Evaluation Status" });
            }
        }
        public async Task<ResultResponseDto<bool>> RegeneratePillarAiSearch(RegeneratePillarAiSearchDto channel, int userID, UserRole userRole)
        {
            try
            {
                if (channel.QuestionEnable)
                {
                    var currentYear = DateTime.Now.Year;
                    var aiQuestionList = await _context.AIEstimatedQuestionScores.Where(x => x.CountryID == channel.CountryID && x.PillarID== channel.PillarID && x.Year == currentYear).ToListAsync();
                    if (aiQuestionList.Count > 0)
                    {
                        _context.AIEstimatedQuestionScores.RemoveRange(aiQuestionList);
                        await _context.SaveChangesAsync();
                    }                   
                    await _iAIAnalayzeService.AnalyzeQuestionsOfCountryPillar(channel.CountryID, channel.PillarID);
                                       
                }

                if (channel.PillarEnable)
                    await _iAIAnalayzeService.AnalyzeSinglePillar(channel.CountryID,channel.PillarID);
                if (!channel.QuestionEnable && channel.RegenerateMissingQuestionsEnable)
                {
                    var request = new MissingCountryQuestionRequest
                    {
                        CountryID = channel.CountryID,
                        PillarID = channel.PillarID
                    };
                    await _iAIAnalayzeService.AnalyzeCountryMissingQuestions(request);
                }

                var msglist = new List<string>
                {
                    "AI research import has been initiated successfully"
                };
               
                await _context.SaveChangesAsync();
                return ResultResponseDto<bool>.Success(true, msglist);
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error in RegenerateAiSearch", ex);

                return ResultResponseDto<bool>.Failure(new[] { "Something went wrong while importing AI research. Please try again later." });
            }
        }

        private void ApplyCountryRanking(List<AiCountrySummeryDto> countriesDetails, List<dynamic> countryRanks, string reportType = "AI")
        {
            var totalCountryCount = countriesDetails.Count;

            // Recalculate global ranks safely
            var orderedGlobalRanks = countryRanks
                .OrderByDescending(x => reportType == "AI"
                    ? (decimal)(x.AiProgress ?? 0)
                    : (decimal)(x.ScoreProgress ?? 0))
                .Select((x, index) => new
                {
                    x.CountryID,
                    Rank = index + 1
                })
                .ToList();

            var countryRankLookup = orderedGlobalRanks
                .ToDictionary(x => x.CountryID);

            // Region-wise ranking
            var regionRankLookup = new Dictionary<int, (int Rank, int TotalCountry)>();

            var regionGroups = countriesDetails
                .GroupBy(x => x.Region);

            foreach (var region in regionGroups)
            {
                var regionCountryIDs = region
                    .Select(x => x.CountryID)
                    .ToHashSet();

                var rankedCountries = countryRanks
                    .Where(x => regionCountryIDs.Contains((int)x.CountryID))
                    .OrderByDescending(x => reportType == "AI"
                        ? (decimal?)(x.AiProgress ?? 0)
                        : (decimal?)(x.ScoreProgress ?? 0))
                    .Select((x, index) => new
                    {
                        x.CountryID,
                        Rank = index + 1
                    })
                    .ToList();

                var regionTotal = rankedCountries.Count;

                foreach (var country in rankedCountries)
                {
                    regionRankLookup[(int)country.CountryID] =
                        (country.Rank, regionTotal);
                }
            }

            // Assign final values
            foreach (var country in countriesDetails)
            {
                if (countryRankLookup.TryGetValue(country.CountryID, out var globalRank))
                {
                    country.Rank = globalRank.Rank;
                    country.TotalCountry = totalCountryCount;
                }

                if (regionRankLookup.TryGetValue(country.CountryID, out var regionRank))
                {
                    country.RegionRank = regionRank.Rank;
                    country.RegionTotalCountry = regionRank.TotalCountry;
                }
            }
        }

        private List<dynamic> CalculateCountryRanks(List<EvaluationCountryProgressResultDto> progress, decimal pillarCount, string reportType = "AI")
        {
            var groupedProgress = progress
                .GroupBy(x => x.CountryID)
                .Select(g => new
                {
                    CountryID = g.Key,
                    ScoreProgress = Math.Round((g.Select(x => x.ScoreProgress).DefaultIfEmpty(0).Sum()) / pillarCount, 2),
                    AiProgress = g.Select(x => x.AIProgress).DefaultIfEmpty(0).Average()
                });

            return groupedProgress
                .OrderByDescending(x => reportType == "AI" ? x.AiProgress : x.ScoreProgress)
                .Select((x, index) => new
                {
                    x.CountryID,
                    x.ScoreProgress,
                    x.AiProgress,
                    Rank = index + 1
                })
                .Cast<dynamic>()
                .ToList();
        }
        public async Task<AiCountrySummeryDto> GetCountryAiSummeryDetail(int userID, UserRole userRole, int? countryID, int year, string reportType = "AI")
        {
            reportType = reportType.ToUpper();
            var query = await GetCountryAiSummeryDetails(userID, userRole, null, year);
            var countriesDetails = await query.ToListAsync();

            var progress = await _commonService.GetCountriesProgressAsync(userID, (int)userRole, DateTime.Now.Year);
            var analyticalLayers = _context.AnalyticalLayers.AsQueryable();
            var totalValidKpis = await analyticalLayers.Distinct().CountAsync();

            int pillarCount = _appSettings.PillarCount;
            var countryRanks = CalculateCountryRanks(progress, pillarCount, reportType);

            ApplyCountryRanking(countriesDetails, countryRanks, reportType);

            var countryDetails = countriesDetails.FirstOrDefault(x=>x.CountryID==countryID);

            if (countryDetails !=null && userRole != UserRole.CountryUser && countryID.HasValue )
            {
                var score = countryDetails.AIProgress;
                
                var countryProgress = progress.Where(x => x.CountryID == countryID);

                var countryScore = countryProgress
                    .Select(x => x.ScoreProgress)
                    .DefaultIfEmpty(0)
                    .Sum();

                countryScore = Math.Round(countryScore / (decimal)pillarCount, 2);

                countryDetails.EvaluatorProgress = countryScore;
                countryDetails.Discrepancy = Math.Abs(countryScore - (countryDetails.AIProgress ?? 0));
                if (reportType != "AI")
                {
                    score = countryDetails.EvaluatorProgress;
                }
                countryDetails.EvidenceSummary = CommonService.InitailLineOfExecutiveSummery(countryDetails.EvidenceSummary, countryDetails.ImmediateSituationSummary, score, countryDetails.CountryName, pillarCount, totalValidKpis);
            }

            return countryDetails ?? new AiCountrySummeryDto();
        }

        public async Task<List<AiCountrySummeryDto>> GetAllCountryAiSummeryDetail(int userID, UserRole userRole, int year)
        {
            var query = await GetCountryAiSummeryDetails(userID, userRole, null, year);
            var countriesDetails = await query.ToListAsync();
            var analyticalLayers = _context.AnalyticalLayers.AsQueryable();
            var totalValidKpis = await analyticalLayers.Distinct().CountAsync();
            int pillarCount = _appSettings.PillarCount;
            var progress = await _commonService.GetCountriesProgressAsync(userID, (int)userRole, DateTime.Now.Year);

            var countryRanks = CalculateCountryRanks(progress, pillarCount
                );

            ApplyCountryRanking(countriesDetails, countryRanks);

            if (userRole != UserRole.CountryUser)
            {
                foreach (var country in countriesDetails)
                {
                    country.EvidenceSummary = CommonService.InitailLineOfExecutiveSummery(country.EvidenceSummary,
                    country.ImmediateSituationSummary, country.AIProgress, country.CountryName, pillarCount, totalValidKpis);
                    var cr = countryRanks.FirstOrDefault(x => x.CountryID == country.CountryID);

                    if (cr != null)
                    {
                        country.EvaluatorProgress = Math.Round(cr.ScoreProgress, 2);
                        country.Discrepancy = Math.Abs(cr.ScoreProgress - (country.AIProgress ?? 0));
                    }
                }
            }

            return countriesDetails;
        }

        #endregion

        #region pdf pillars and country report

        private async Task<List<KpiChartItem>> GetAccessKpis(int userID, UserRole role, int? countryID, int year = 0, bool isAiScore =true)
        {
            var startDate = new DateTime(year, 1, 1);
            var endDate = new DateTime(year + 1, 1, 1);

            var baseQuery = _context.AnalyticalLayerResults
                .AsNoTracking()
                .Include(ar => ar.AnalyticalLayer)
                    .ThenInclude(al => al.FiveLevelInterpretations)
                .Include(ar => ar.Country)
                .Where(x => x.AiLastUpdated >= startDate && x.AiLastUpdated < endDate);

            if (role == UserRole.CountryUser)
            {
                var validCountries = _context.PublicUserCountryMappings
                    .Where(x => x.IsActive && x.UserID == userID)
                    .Select(x => x.CountryID);

                var validPillarIds = _context.CountryUserPillarMappings
                    .Where(x => x.IsActive && x.UserID == userID)
                    .Select(x => x.PillarID);

                var validLayerIds = _context.AnalyticalLayerPillarMappings
                    .Where(x => validPillarIds.Contains(x.PillarID))
                    .Select(x => x.LayerID)
                    .Distinct();

                baseQuery = baseQuery
                    .Where(ar =>
                        validCountries.Contains(ar.CountryID) &&
                        validLayerIds.Contains(ar.LayerID));
            }

            var kpiRaw = baseQuery
            .Where(x => !countryID.HasValue || x.CountryID == countryID)
            .Select(x => new
            {
                KpiShortName = x.AnalyticalLayer.LayerCode,
                KpiName = x.AnalyticalLayer.LayerName,
                CountryID = x.CountryID,
                AiCalValue5 = x.AiCalValue5,
                CalValue5 = x.CalValue5,
                Definition=x.AnalyticalLayer.Definition,
                AnalyticalLayer = x.AnalyticalLayer
            })
            .Select(x => new
            {
                x.KpiShortName,
                x.KpiName,
                x.CountryID,
                x.AiCalValue5,
                x.CalValue5,
                LayerID = x.AnalyticalLayer.LayerID,
                Definition = x.Definition,
                Interpretation = x.AnalyticalLayer.FiveLevelInterpretations.Select(i => new FiveLevelInterpretationsDto
                (
                   i.InterpretationID,
                   i.LayerID,
                   i.MinRange,
                   i.MaxRange,
                   i.Condition,
                   i.StrategicAction
                )).ToList()

            }).OrderBy(x=>x.LayerID);

            var kpis = await kpiRaw
                .Select(k => new KpiChartItem(k.KpiShortName, k.KpiName, isAiScore && role == UserRole.Admin ? k.AiCalValue5 : k.CalValue5, k.Definition, k.CountryID,k.Interpretation))
                .ToListAsync();

            return kpis ?? new List<KpiChartItem>();
        }
        private async Task<List<PeerCountryHistoryReportDto>> GetpeerCountries(int userID, UserRole role, int countryID, int year , bool isAiScore =true)
        {
            var peerCountries = new List<PeerCountryHistoryReportDto>();
            var pillarCount = _appSettings.PillarCount;

            var peersCountryIds = await _context.Countries
                   .Where(x => x.CountryID == countryID && x.IsActive && !x.IsDeleted)
                   .SelectMany(x => x.CountryPeers)
                   .Where(x => x.IsActive && !x.IsDeleted)
                   .Select(x => x.PeerCountryID)
                   .ToListAsync();
            if (peersCountryIds.Count > 0)
            {
                peersCountryIds.Add(countryID);
            }

            var startYear = year - 5;

            peerCountries = await _context.Countries
                .Where(c => peersCountryIds.Contains(c.CountryID))
                .Select(c => new PeerCountryHistoryReportDto
                {
                    CountryID = c.CountryID,
                    CountryName = c.CountryName,
                    Continent = c.Continent,                   
                    Region = c.Region,
                    CountryCode = c.CountryCode,
                    UpdatedDate = c.UpdatedDate,
                    Image = c.Image,
                    Latitude = c.Latitude,
                    Longitude = c.Longitude,
                    Population = c.Population,
                    Income = c.Income                
                }).ToListAsync();

            if (isAiScore)
            {
                foreach(var c in peerCountries)
                {
                    c.CountryHistory = _context.AIPillarScores
                    .Include(x => x.Pillar)
                    .Where(x =>
                        x.CountryID == c.CountryID &&
                        x.Year >= startYear &&
                        x.Year <= year)
                    .GroupBy(x => x.Year)
                    .Select(yearGroup => new PeerCountryYearHistoryDto
                    {
                        CountryID = c.CountryID,
                        Year = yearGroup.Key,

                        ScoreProgress = yearGroup.Average(x => x.AIProgress ?? 0),

                        Pillars = yearGroup
                            .GroupBy(p => new
                            {
                                p.PillarID,
                                p.Pillar.PillarName,
                                p.Pillar.DisplayOrder
                            })
                            .Select(pillarGroup => new PeerCountryPillarHistoryReportDto
                            {
                                PillarID = pillarGroup.Key.PillarID,
                                PillarName = pillarGroup.Key.PillarName,
                                DisplayOrder = pillarGroup.Key.DisplayOrder,
                                ScoreProgress = pillarGroup.Average(x => x.AIProgress ?? 0)
                            })
                            .OrderBy(x => x.DisplayOrder)
                            .ToList()
                    })
                    .OrderBy(x => x.Year)
                    .ToList();
                }
            }
            else
            {
                var pillars = await _context.Pillars.Select(x => new
                {
                    x.PillarID,
                    x.PillarName,
                    x.DisplayOrder
                }).ToListAsync();

                var countryProgress = await _commonService
                    .GetCountriesProgressHistoryAsync(userID, (int)role, year - 5, year);

                var filterCites = countryProgress
                    .Where(x => peersCountryIds.Contains(x.CountryID))
                    .ToList();

                foreach (var country in peerCountries)
                {
                    var progress = filterCites
                        .Where(x => x.CountryID == country.CountryID)
                        .ToList();

                    // ✅ Build Year-wise history first
                    country.CountryHistory = progress
                        .GroupBy(x => x.Year)
                        .Select(yearGroup => new PeerCountryYearHistoryDto
                        {
                            CountryID = country.CountryID,
                            Year = yearGroup.Key,

                            // Country level score
                            ScoreProgress = Math.Round(
                                yearGroup.Select(x => x.ScoreProgress)
                                         .DefaultIfEmpty(0)
                                         .Sum()/ (decimal)pillarCount, 2),

                            // Pillar level score
                            Pillars = pillars
                                .Select(p => new PeerCountryPillarHistoryReportDto
                                {
                                    PillarID = p.PillarID,
                                    PillarName = p.PillarName,
                                    DisplayOrder = p.DisplayOrder,

                                    ScoreProgress = Math.Round(
                                        yearGroup
                                            .Where(x => x.PillarID == p.PillarID)
                                            .Select(x => x.ScoreProgress)
                                            .DefaultIfEmpty(0)
                                            .Average(), 2)
                                })
                                .OrderBy(x => x.DisplayOrder)
                                .ToList()
                        })
                        .OrderBy(x => x.Year)
                        .ToList();
                }
            }                

            return peerCountries;
        }

        // ─────────────────────────────────────────────────────────────────────────────
        //  ENTRY POINTS  (GenerateCountryDetailsPdf / GeneratePillarDetailsPdf)
        // ─────────────────────────────────────────────────────────────────────────────

        public async Task<byte[]> GenerateCountryDetailsReport(AiCountrySummeryDto countryDetails, UserRole userRole, int userID, 
            Common.Interface.DocumentFormat format = Common.Interface.DocumentFormat.Pdf, string reportType ="ai")
        {
            try
            {
                var isAi = !(reportType != "ai" && userRole == UserRole.Admin);

                var pillars = await GetAICountryPillars(countryDetails.CountryID, userID, userRole, countryDetails.ScoringYear);

                var kpis = await GetAccessKpis(userID, userRole, countryDetails.CountryID, countryDetails.ScoringYear, isAi);

                if (!isAi)
                {
                    countryDetails.AIProgress = countryDetails.EvaluatorProgress;

                    countryDetails.EvidenceSummary = _commonService.ReplacePercentAcross(countryDetails.EvidenceSummary, (int)(countryDetails.EvaluatorProgress ?? 0));

                    foreach (var pillar in pillars.Result.Pillars)
                    {
                        pillar.AIProgress = pillar.EvaluatorProgress;
                        pillar.DataGapAnalysis = pillar.AnalystDataGapAnalysis;
                    }
                }

                var peerCountries = await GetpeerCountries(userID, userRole, countryDetails.CountryID, countryDetails.ScoringYear, isAi);


                var document = await _documentGeneratorService.GenerateCountryDetails(countryDetails, pillars.Result.Pillars, kpis, peerCountries, userRole, format);

                return document;
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occured in GenerateCountryDetailsReport", ex);
                return Array.Empty<byte>();
            }
        }

        public async Task<byte[]> GeneratePillarDetailsReport(AiCountryPillarReponse pillarData, UserRole userRole, Common.Interface.DocumentFormat format = Common.Interface.DocumentFormat.Pdf)
        {
            try
            {
                var document = await _documentGeneratorService.GeneratePillarDetails(pillarData, userRole, format);


                return document;
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occured in GeneratePillarDetailsReport", ex);
                return Array.Empty<byte>();
            }
        }

        public async Task<byte[]> GenerateAllCountryDetailsReport(List<AiCountrySummeryDto> countriesDetails, UserRole userRole, int userID, int year, Common.Interface.DocumentFormat format = Common.Interface.DocumentFormat.Pdf)
        {
            try
            {
                var pillars = await GetAllCountriesAIPillars(userID, userRole, year);

                var kpis = new List<KpiChartItem>();

                var recordAvailable = pillars.Result.Any(x => countriesDetails.Select(x => x.CountryID).Contains(x.Key));
                if (recordAvailable)
                {
                    var document = await _documentGeneratorService.GenerateAllCountriesDetails(countriesDetails, pillars.Result, kpis, userRole, format);

                    return document;
                }
                else
                {
                    return Array.Empty<byte>();
                }
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occured in GenerateCountryDetailsReport", ex);
                return Array.Empty<byte>();
            }
        }

        #endregion pdf pillars and country report

        #region TransferAssessment
        public async Task<ResultResponseDto<string>> AITransferAssessment(AITransferAssessmentRequestDto r, int userID, UserRole userRole)
        {
            try
            {
                var currentDate = DateTime.Now;
                var year = currentDate.Year;

                if (userRole == UserRole.CountryUser || userRole == UserRole.Evaluator)
                {
                    return ResultResponseDto<string>.Failure(new[] { "Failed to transfer assessment, You don't have access." });
                }

                if(userRole == UserRole.Analyst)
                {
                    r.TransferToUserID = userID;

                    var validCountry = _context.UserCountryMappings.Any(x => !x.IsDeleted && x.CountryID == r.CountryID && x.UserID == userID);

                    if (!validCountry)
                    {
                        return ResultResponseDto<string>.Failure(new[] { "This assessment can’t be imported because the selected user hasn’t been assigned to this country yet." });
                    }
                }

                var aiAssessmentData = await _context.AIEstimatedQuestionScores
                                    .Where(x => x.CountryID == r.CountryID && x.Year == year)
                                    .ToListAsync();

                var aiAssessmentQuestions = aiAssessmentData
                    .GroupBy(x => x.PillarID)
                    .ToDictionary(g => g.Key, g => g.ToList());

                if (aiAssessmentQuestions == null || aiAssessmentQuestions.Count==0)
                    return ResultResponseDto<string>.Failure(new[] { "There is no ai assessment is available for this country" });


                var userCountryMapping = await _context.UserCountryMappings.FirstOrDefaultAsync(x => !x.IsDeleted && x.CountryID == r.CountryID && x.UserID == r.TransferToUserID);

                if(userCountryMapping == null)
                    return ResultResponseDto<string>.Failure(new[] { "This assessment can’t be imported because the selected user hasn’t been assigned to this country yet." });


                // Load existing assessment for that user/country/year (with pillars/responses)
                var existingAssessment = await _context.Assessments
                    .Include(a => a.PillarAssessments)
                        .ThenInclude(p => p.Responses)
                    .FirstOrDefaultAsync(a => a.UserCountryMappingID == userCountryMapping.UserCountryMappingID &&
                                              a.UpdatedAt.Year == year);

                if (existingAssessment == null)
                {
                    existingAssessment = new Assessment
                    {
                        UserCountryMappingID = userCountryMapping.UserCountryMappingID,
                        CreatedAt = currentDate,
                        UpdatedAt = currentDate,
                        IsActive = true,
                        AssessmentPhase = userRole == UserRole.Admin ? AssessmentPhase.Completed: AssessmentPhase.InProgress,
                        PillarAssessments = new List<PillarAssessment>()
                    };

                    _context.Assessments.Add(existingAssessment);
                }
                else
                {
                    existingAssessment.UpdatedAt = currentDate;
                    existingAssessment.AssessmentPhase =  AssessmentPhase.InProgress;
                }

                var questions = await _context.Questions.Include(x=>x.QuestionOptions).ToDictionaryAsync(q => q.QuestionID, q => q);

                // Transfer pillar data
                foreach (var pillar in aiAssessmentQuestions)
                {
                    var existingPillar = existingAssessment.PillarAssessments
                        .FirstOrDefault(x => x.PillarID == pillar.Key);

                    if (existingPillar == null)
                    {
                        existingPillar = new PillarAssessment
                        {
                            PillarID = pillar.Key,
                            Responses = new List<AssessmentResponse>()
                        };
                        existingAssessment.PillarAssessments.Add(existingPillar);
                    }

                    // Add/Update responses
                    foreach (var response in pillar.Value)
                    {
                        var existingResponse = existingPillar.Responses
                            .FirstOrDefault(rp => rp.QuestionID == response.QuestionID);

                        var qustion = questions.ContainsKey(response.QuestionID) ? questions[response.QuestionID] : null;
                        if (qustion == null)
                            continue;

                        int? score = response.AIScore != null ? (int?)Math.Round(response.AIScore.Value, 0) : null;

                        var option = qustion.QuestionOptions.FirstOrDefault(x => x.ScoreValue == score);
                        if (option == null)
                            continue;

                        if (existingResponse == null)
                        {                           

                            existingPillar.Responses.Add(new AssessmentResponse
                            {
                                QuestionID = response.QuestionID,
                                QuestionOptionID = option.OptionID,
                                Justification = response.EvidenceSummary,
                                Source = response.SourceDataExtract + "SourceURL : " + response.SourceURL,
                                Score = (ScoreValue?)score
                            });
                        }
                        else
                        {
                            existingResponse.QuestionOptionID = option.OptionID;
                            existingResponse.Justification = response.EvidenceSummary;
                            existingResponse.Score = (ScoreValue?)score;
                            existingResponse.Source = response.SourceDataExtract + " SourceURL : " + response.SourceURL;
                        }
                    }

                    // Delete responses not present in transferAssessment
                    var transferQuestionIds = pillar.Value.Select(x => x.QuestionID).ToHashSet();
                    var toDeleteResponses = existingPillar.Responses
                        .Where(x => !transferQuestionIds.Contains(x.QuestionID))
                        .ToList();

                    foreach (var resp in toDeleteResponses)
                    {
                        //existingPillar.Responses.Remove(resp);
                        _context.AssessmentResponses.Remove(resp);
                    }
                }

                // Delete pillars not present in transferAssessment
                var transferPillarIds = aiAssessmentQuestions.Select(x => x.Key).ToHashSet();
                var toDeletePillars = existingAssessment.PillarAssessments
                    .Where(x => !transferPillarIds.Contains(x.PillarID))
                    .ToList();

                foreach (var pillar in toDeletePillars)
                {
                    //existingAssessment.PillarAssessments.Remove(pillar);
                    _context.PillarAssessments.Remove(pillar);
                }
                if(existingAssessment.AssessmentPhase == AssessmentPhase.Completed)
                {
                    _download.InsertAnalyticalLayerResults(r.CountryID);
                }
                await _context.SaveChangesAsync();

                return ResultResponseDto<string>.Success("", new[] { "Assessment transferred successfully." });
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error in TransferAssessment", ex);
                return ResultResponseDto<string>.Failure(new[] { "Failed to transfer assessment, please try again later." });
            }
        }
        #endregion TransferAssessment


        public async Task<ResultResponseDto<string>> ReCalculateKpis(int userID, UserRole userRole)
        {
            try
            {
                if (userRole != UserRole.Admin)
                {
                    return ResultResponseDto<string>.Failure(new[] { "Failed to recalculate KPIs, You don't have access." });
                }

                await _context.Database.ExecuteSqlRawAsync("EXEC sp_AiRecalculateCountryScore");

                await _context.Database.ExecuteSqlRawAsync("EXEC sp_InsertAnalyticalLayerResults");

                await _context.Database.ExecuteSqlRawAsync("EXEC sp_AiInsertAnalyticalLayerResults");

                return ResultResponseDto<string>.Success("", new[] { "KPI recalculation has been initiated successfully." } );
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error in ReCalculateKpis", ex);
                return ResultResponseDto<string>.Failure(new[] { "Failed to recalculate KPIs, please try again later." });
            }
        }


        public async Task<ResultResponseDto<string>> UploadAiDocuments(
            UploadAiDocumentRequest request,
            int userID,
            UserRole userRole)
        {
            try
            {
                if (userRole != UserRole.Admin)
                {
                    return ResultResponseDto<string>.Failure(
                        new[] { "Failed to Upload Ai Documents, You don't have access." });
                }

                var basePath = Path.Combine(_env.WebRootPath, "aidocuments");

                if (!Directory.Exists(basePath))
                    Directory.CreateDirectory(basePath);

                for (int i = 0; i < request.Files.Count; i++)
                {
                    var file = request.Files[i];
                    var pillarId = request.PillarIDs[i];

                    var ext = Path.GetExtension(file.FileName).ToLower();

                    if (ext != ".pdf" && ext != ".docx")
                        continue;

                    if (!Directory.Exists(basePath))
                        Directory.CreateDirectory(basePath);

                    var storedFileName = $"{Guid.NewGuid()}{ext}";
                    var fullPath = Path.Combine(basePath, storedFileName);

                    // ✅ Save file
                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    // ✅ Save DB record
                    var doc = new CountryDocument
                    {
                        FileName = file.FileName,
                        StoredFileName = storedFileName,
                        FilePath = fullPath,
                        CountryID = request.CountryID,
                        PillarID = pillarId == 0 ? null : pillarId,
                        FileType = ext,
                        FileSize = file.Length / 1024,//kb file will be store now
                        ProcessingStatus = DocumentProcessingStatus.Pending,
                        UpdatedAt = DateTime.UtcNow,
                        UploadedByUserID = userID,
                        DocumentLevel = GetDocumentLevel(request.CountryID, pillarId)
                    };

                    _context.CountryDocuments.Add(doc);
                    await _context.SaveChangesAsync();
                    await _iAIAnalayzeService.ProcessDocument(doc.CountryDocumentID);
                }

                

                return ResultResponseDto<string>.Success(
                    "",
                    new[] { "Upload Ai Documents has been initiated successfully." });
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error in Upload Ai Documents", ex);

                return ResultResponseDto<string>.Failure(
                    new[] { "Failed to Upload Ai Documents, please try again later." });
            }
        }

        public async Task<PaginationResponse<GetCountryDocumentResponseDto>> GetAICountryDocuments(
            AiCountryDocumentRequestDto request,
            int userID,
            UserRole userRole)
        {
            try
            {
                Expression<Func<UserCountryMapping, bool>> filter = userRole switch
                {
                    UserRole.Admin => x => !x.IsDeleted,
                    UserRole.Analyst => x => !x.IsDeleted && (x.UserID == userID || x.AssignedByUserId == userID),
                    UserRole.Evaluator => x => !x.IsDeleted && x.UserID == userID,
                    _ => x => false
                };

                var userCountryIDs = await _context.UserCountryMappings
                    .Where(filter)
                    .Select(x => x.CountryID)
                    .Distinct()
                    .ToListAsync();

                var query = _context.Countries
                     .Where(c =>
                        (
                            !request.CountryID.HasValue
                            || c.CountryID == request.CountryID
                        )
                        && (userCountryIDs.Contains(c.CountryID) || userRole == UserRole.Admin)
                        && c.IsActive
                        && !c.IsDeleted
                    )
                    .Select(x => new GetCountryDocumentResponseDto 
                    {
                        CountryID = x.CountryID,
                        CountryName = x.CountryName,
                        FileTypes = ""
                    });

                var result = await query.ApplyPaginationAsync(request);

                // 🔥 FileTypes (optimized for selected countries only)
                var CountryIDs = result.Data.Select(x => x.CountryID).ToList();

                var fileTypesData = await _context.CountryDocuments.Where(x =>!x.IsDeleted && x.CountryID.HasValue &&
                                     CountryIDs.Contains(x.CountryID.Value)).GroupBy(x => x.CountryID)
                                    .Select(g => new
                                    {
                                        CountryID = g.Key,
                                        FileTypes = g.Select(x => x.FileType).Distinct().ToList(),

                                        NoOfFiles = g.Count(),
                                        NoOfUsers = g.Select(d => d.UploadedByUserID).Distinct().Count(),
                                        FilesSize = g.Sum(d => (long?)d.FileSize) ?? 0,
                                    })
                            .ToListAsync();

                foreach (var item in result.Data)
                {
                    var ft = fileTypesData.FirstOrDefault(x => x.CountryID == item.CountryID);
                    if (ft != null)
                    {
                        item.FileTypes = string.Join(", ", ft.FileTypes);
                        item.NoOfFiles = ft.NoOfFiles;
                        item.NoOfUsers = ft.NoOfUsers;
                        item.FilesSize = ft.FilesSize;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occured in GetAIDocuments", ex);

                return new PaginationResponse<GetCountryDocumentResponseDto>();
            }
        }

        public async Task<ResultResponseDto<List<GetCountryPillarDocumentResponseDto>>> GetAICountryPillarDocuments(
              AiCountryPillarDocumentRequestDto request,
              int userID,
              UserRole userRole)
        {
            try
            {
                var result = await _context.CountryDocuments
                   .Where(x => !x.IsDeleted && (x.CountryID == request.CountryID || x.CountryID == null))
                   .Select(x => new GetCountryPillarDocumentResponseDto
                   {
                       CountryDocumentID = x.CountryDocumentID,
                       CountryID = x.CountryID,
                       PillarID = x.PillarID,

                       PillarName = x.CountryID.HasValue ? _context.Pillars
                           .Where(p => p.PillarID == x.PillarID)
                           .Select(p => p.PillarName)
                           .FirstOrDefault() : x.DocumentLevel,

                       FileName = x.FileName,
                       FilePath = x.FilePath,
                       FileSize = x.FileSize,
                       FileType = x.FileType,
                       ProcessingStatus = x.ProcessingStatus,
                       StoredFileName = x.StoredFileName,

                       UploadedBy = "",
                       UploadedByUserID = x.UploadedByUserID ?? 0
                   })
                   .OrderBy(x => x.PillarID)
                   .ToListAsync();

                var users = _context.Users
                    .Where(x => result.Select(x => x.UploadedByUserID)
                    .Contains(x.UserID) && !x.IsDeleted)
                    .ToDictionary(x => x.UserID, y => y.FullName);

                foreach (var r in result)
                {
                    r.UploadedBy = users.TryGetValue(r.UploadedByUserID, out var userName) ? userName : "";
                }

                return ResultResponseDto<List<GetCountryPillarDocumentResponseDto>>.Success(result, new[] { "Get documents successfully" });
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occured in GetAIDocuments", ex);
                return ResultResponseDto<List<GetCountryPillarDocumentResponseDto>>.Failure(new[] { "Failed to get Documents, please try again later." });
            }
        }

        public async Task<ResultResponseDto<string>> DeleteDocument(
            DeleteCountryDocumentRequestDto request,
            int userID,
            UserRole userRole)
        {
            try
            {
                var query = _context.CountryDocuments
                    .Where(x => !x.IsDeleted && x.CountryID == request.CountryID);

                // 🔷 If not admin → only own documents
                if (userRole != UserRole.Admin)
                {
                    query = query.Where(x => x.UploadedByUserID == userID && (!request.CountryDocumentID.HasValue || x.CountryDocumentID == request.CountryDocumentID));
                }

                // 🔷 If NOT delete all → filter by document id
                if (!request.IsAll && request.CountryDocumentID.HasValue)
                {
                    query = query.Where(x => x.CountryDocumentID == request.CountryDocumentID.Value);
                }

                var documents = await query.ToListAsync();

                if (!documents.Any())
                {
                    return ResultResponseDto<string>.Failure(
                        new[] { "No documents found or you don't have permission." });
                }

                // 🔥 Soft delete
                foreach (var doc in documents)
                {
                    doc.IsDeleted = true;
                    await _iAIAnalayzeService.DeleteDocument(doc.CountryDocumentID);
                }

                await _context.SaveChangesAsync();

                return ResultResponseDto<string>.Success(
                    "",
                    new[] { "Document(s) deleted successfully." });
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error Occured in DeleteDocument", ex);

                return ResultResponseDto<string>.Failure(
                    new[] { "Failed to delete document, please try again later." });
            }
        }
        public async Task<FileResult> DownloadDocument(int countryDocumentID, int userID, UserRole userRole)
        {
            var doc = await _context.CountryDocuments
                .FirstOrDefaultAsync(x => x.CountryDocumentID == countryDocumentID && !x.IsDeleted);

            if (doc == null)
                throw new Exception("Document not found.");

            if (!System.IO.File.Exists(doc.FilePath))
                throw new Exception("File not found on server.");

            var ext = Path.GetExtension(doc.FileName).ToLower();

            var contentType = ext switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream"
            };

            var stream = new FileStream(doc.FilePath, FileMode.Open, FileAccess.Read);

            return new FileStreamResult(stream, contentType)
            {
                FileDownloadName = doc.FileName
            };
        }
        static string GetDocumentLevel(int? countryID, int? pillarID)
        {
            if (countryID == null)
            {
                return "Global";
            }
            else if (countryID > 0 && (pillarID == null || pillarID == 0))
            {
                return "Country_Pillar";
            }
            else
            {
                return "Country";
            }
        }

    }
  
    public record PillarChartItem(string ShortName, string Name, decimal? Value);
    public record KpiChartItem(string ShortName, string Name, decimal? Value,string? Definition, int? CountryID, List<FiveLevelInterpretationsDto> InterPretation);
    public record FiveLevelInterpretationsDto ( 
         int InterpretationID ,
         int LayerID ,
         decimal? MinRange ,
         decimal? MaxRange ,
         string Condition ,
         string StrategicAction 
    );


}

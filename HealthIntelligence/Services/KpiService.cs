using HealthIntelligence.Common.Implementation;
using HealthIntelligence.Common.Models;
using HealthIntelligence.Data;
using HealthIntelligence.Dtos.CountryUserDto;
using HealthIntelligence.Dtos.CommonDto;
using HealthIntelligence.Dtos.kpiDto;
using HealthIntelligence.Enums;
using HealthIntelligence.IServices;
using HealthIntelligence.Models;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace HealthIntelligence.Services
{
    public class KpiService : IKpiService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAppLogger _appLogger;
        public KpiService(ApplicationDbContext context, IAppLogger appLogger)
        {
            _context = context;
            _appLogger = appLogger;
        }

        #region GetAnalyticalLayerResults
        public async Task<PaginationResponse<GetAnalyticalLayerResultDto>> 
            GetAnalyticalLayerResults(GetAnalyticalLayerRequestDto request, int userId, UserRole role, TieredAccessPlan userPlan = TieredAccessPlan.Pending)
        {
            try
            {
                var year = request.Year;
                var startDate = new DateTime(year, 1, 1);
                var endDate = new DateTime(year + 1, 1, 1);

                var baseQuery = _context.AnalyticalLayerResults
                    .AsNoTracking()
                    .Include(ar => ar.AnalyticalLayer)
                        .ThenInclude(al => al.FiveLevelInterpretations)
                    .Include(ar => ar.Country)
                    .Where(x => (x.LastUpdated >= startDate && x.LastUpdated < endDate) || (x.AiLastUpdated >= startDate && x.AiLastUpdated < endDate));

                if (role == UserRole.CountryUser)
                {
                    var validCountries = _context.PublicUserCountryMappings
                        .Where(x =>
                            x.IsActive &&
                            x.UserID == userId &&
                            (!request.CountryID.HasValue || x.CountryID == request.CountryID))
                        .Select(x => x.CountryID);

                    var validPillarIds = _context.CountryUserPillarMappings
                        .Where(x => x.IsActive && x.UserID == userId)
                        .Select(x => x.PillarID);

                    var validLayerIds = _context.AnalyticalLayerPillarMappings
                        .Where(x =>
                            validPillarIds.Contains(x.PillarID) &&
                            (!request.LayerID.HasValue || x.LayerID == request.LayerID))
                        .Select(x => x.LayerID)
                        .Distinct();

                    baseQuery = baseQuery
                        .Where(ar =>
                            validCountries.Contains(ar.CountryID) &&
                            validLayerIds.Contains(ar.LayerID));
                }
                else if (role == UserRole.Evaluator || role == UserRole.Analyst)
                {
                    var validCountries = _context.UserCountryMappings
                        .Where(x =>
                            x.UserID == userId &&
                            !x.IsDeleted &&
                            (!request.CountryID.HasValue || x.CountryID == request.CountryID))
                        .Select(x => x.CountryID);
                    baseQuery = baseQuery.Where(ar => validCountries.Contains(ar.CountryID) && (!request.LayerID.HasValue || ar.LayerID == request.LayerID));
                }
                else
                {
                    baseQuery = baseQuery.Where(ar =>
                        (!request.CountryID.HasValue || ar.CountryID == request.CountryID) &&
                        (!request.LayerID.HasValue || ar.LayerID == request.LayerID));
                }
                var response = await baseQuery.Select(Projection).ApplyPaginationAsync(request);

                return response;

            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error occurred in GetAnalyticalLayers", ex);
                return new PaginationResponse<GetAnalyticalLayerResultDto>();
            }
        }

        private static Expression<Func<AnalyticalLayerResult, GetAnalyticalLayerResultDto>> Projection => ar => new GetAnalyticalLayerResultDto
        {
            LayerResultID = ar.LayerResultID,
            LayerID = ar.LayerID,
            CountryID = ar.CountryID,
            InterpretationID = ar.InterpretationID,
            NormalizeValue = ar.NormalizeValue,
            CalValue1 = ar.CalValue1,
            CalValue2 = ar.CalValue2,
            CalValue3 = ar.CalValue3,
            CalValue4 = ar.CalValue4,
            CalValue5 = ar.CalValue5,
            LastUpdated = ar.LastUpdated,

            AiInterpretationID = ar.AiInterpretationID,
            AiNormalizeValue = ar.AiNormalizeValue,
            AiCalValue1 = ar.AiCalValue1,
            AiCalValue2 = ar.AiCalValue2,
            AiCalValue3 = ar.AiCalValue3,
            AiCalValue4 = ar.AiCalValue4,
            AiCalValue5 = ar.AiCalValue5,
            AiLastUpdated = ar.AiLastUpdated,

            LayerCode = ar.AnalyticalLayer.LayerCode,
            LayerName = ar.AnalyticalLayer.LayerName,
            Purpose = ar.AnalyticalLayer.Purpose,
            CalText1 = ar.AnalyticalLayer.CalText1,
            CalText2 = ar.AnalyticalLayer.CalText2,
            CalText3 = ar.AnalyticalLayer.CalText3,
            CalText4 = ar.AnalyticalLayer.CalText4,
            CalText5 = ar.AnalyticalLayer.CalText5,
            Definition = ar.AnalyticalLayer.Definition,
            FiveLevelInterpretations = ar.AnalyticalLayer.FiveLevelInterpretations,

            Country = ar.Country
        };

        #endregion
        public async Task<ResultResponseDto<List<AnalyticalLayer>>> GetAllKpi(int userId, UserRole role)
        {
            try
            {
                IQueryable<AnalyticalLayer> query = _context.AnalyticalLayers
                    .Where(x => !x.IsDeleted);

                if (role == UserRole.CountryUser)
                {
                    query =
                        from layer in _context.AnalyticalLayers
                        join map in _context.AnalyticalLayerPillarMappings
                            on layer.LayerID equals map.LayerID
                        join userMap in _context.CountryUserPillarMappings
                            on map.PillarID equals userMap.PillarID
                        where !layer.IsDeleted
                              && userMap.IsActive
                              && userMap.UserID == userId
                        select layer;
                }

                var result = await query
                    .AsNoTracking()
                    .Distinct()
                    .ToListAsync();

                return ResultResponseDto<List<AnalyticalLayer>>.Success(result);
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error occurred in GetAllKpi", ex);
                return ResultResponseDto<List<AnalyticalLayer>>.Failure(new List<string> { "An error occurred" });
            }
        }
        public async Task<ResultResponseDto<CompareCountryResponseDto>> CompareCountries(CompareCountryRequestDto c, int userId, UserRole role, bool applyPagination = true)
        {
            try
            {
                var year = c.UpdatedAt.Year;
                var startDate = new DateTime(year, 1, 1);
                var endDate = new DateTime(year + 1, 1, 1);


                var validKpiIds = new List<int>();

                if (c.Kpis==null || c.Kpis.Count == 0)
                {
                    var query = _context.AnalyticalLayers
                        .Where(x => !x.IsDeleted)
                        .Select(x => x.LayerID)
                        .OrderBy(x => x);

                    if (applyPagination)
                    {
                        var res = await query.ApplyPaginationAsync(c);
                        validKpiIds = res.Data.ToList();
                    }
                    else
                    {
                        validKpiIds = await query.ToListAsync();
                    }
                }
                else
                {
                    validKpiIds = c.Kpis;
                }

                Expression<Func<Country, bool>> expression = role switch
                {
                    UserRole.Admin => x => !x.IsDeleted && c.Countries.Contains(x.CountryID),
                    UserRole.Analyst => x => !x.IsDeleted && c.Countries.Contains(x.CountryID),
                    UserRole.Evaluator => x => !x.IsDeleted && c.Countries.Contains(x.CountryID),
                    _ => x => false
                };

                // Step 2: Get all selected countries (even if no analytical data)
                var selectedCountries = await _context.Countries
                    .Where(expression)
                    .Distinct()
                    .ToListAsync();

                var selectedCountryIDs = selectedCountries.Select(x => x.CountryID).ToList();

                if (role == UserRole.Analyst || role == UserRole.Evaluator)
                {
                    var validMappedCountryIDs = await _context.UserCountryMappings
                       .Where(x => x.UserID == userId && !x.IsDeleted)
                       .Select(x => x.CountryID)
                       .ToListAsync();

                    // ✅ Check if all selected countries are valid
                    bool allValid = selectedCountryIDs.All(id => validMappedCountryIDs.Contains(id));

                    if (!allValid)
                    {
                        return ResultResponseDto<CompareCountryResponseDto>.Failure(new List<string> { "No valid countries found." });
                    }
                }

                // Step 3: Fetch analytical layer results for selected countries
                var analyticalResults = await _context.AnalyticalLayerResults
                    .Include(ar => ar.AnalyticalLayer)
                    .Where(x => selectedCountryIDs.Contains(x.CountryID)
                    && ((x.AiLastUpdated >= startDate && x.AiLastUpdated < endDate || x.LastUpdated >= startDate && x.LastUpdated < endDate))
                    && validKpiIds.Contains(x.LayerID))
                    .Select(ar => new
                    {
                        ar.CountryID,
                        ar.LayerID,
                        ar.AnalyticalLayer.LayerCode,
                        ar.AnalyticalLayer.LayerName,
                        ar.AnalyticalLayer.Definition,
                        ar.CalValue5,
                        ar.AiCalValue5
                    })
                    .ToListAsync();

                // Step 4: Get all distinct layers
                var allLayers = analyticalResults
                    .Select(x => new { x.LayerID, x.LayerCode, x.LayerName,x.Definition })
                    .Distinct()
                    .OrderBy(x => x.LayerName)
                    .ToList();

                // Step 5: Prepare response DTO
                var response = new CompareCountryResponseDto
                {
                    Categories = new List<string>(),
                    Series = new List<ChartSeriesDto>(),
                    TableData = new List<ChartTableRowDto>()
                };

                // Initialize chart series for each country
                foreach (var country in selectedCountries)
                {
                    response.Series.Add(new ChartSeriesDto
                    {
                        Name = country.CountryName,
                        Data = new List<decimal>(),
                        AiData = new List<decimal>()
                    });
                }

                // Add Peer Country Score series
                var peerSeries = new ChartSeriesDto
                {
                    Name = "Peer Country Score",
                    Data = new List<decimal>(),
                    AiData = new List<decimal>()
                };

                // Step 6: Build chart and table data
                foreach (var layer in allLayers)
                {
                    response.Categories.Add(layer.LayerCode);

                    // Map KPI values for each city (0 if missing)
                    var values = new Dictionary<int, List<decimal>>();

                    foreach (var country in selectedCountries)
                    {
                        var value = analyticalResults
                            .FirstOrDefault(r => r.CountryID == country.CountryID && r.LayerID == layer.LayerID);

                        var evaluatedValue = Math.Round(value?.CalValue5 ?? 0, 2);
                        var aiValue = Math.Round(value?.AiCalValue5 ?? 0, 2);
                        values[country  .CountryID] = new List<decimal> { evaluatedValue, aiValue };

                        // Add to series
                        var countrySeries = response.Series.First(s => s.Name == country.CountryName);
                        countrySeries.Data.Add(evaluatedValue);

                        countrySeries.AiData.Add(aiValue);
                    }
                    // ✅ Calculate Peer Country Score (average of all countries for this layer)
                    var peerCountryScore = values.Values.Any() ? Math.Round(values.Values.Select(x => x.First()).Average(), 2) : 0;
                    peerSeries.Data.Add(peerCountryScore);
                    var aiPeerCountryScore = values.Values.Any() ? Math.Round(values.Values.Select(x => x.Last()).Average(), 2) : 0;
                    peerSeries.AiData.Add(aiPeerCountryScore);

                    // Add table data
                    response.TableData.Add(new ChartTableRowDto
                    {
                        LayerID = layer.LayerID,
                        LayerCode = layer.LayerCode,
                        LayerName = layer.LayerName,
                        Definition = layer.Definition,
                        CountryValues = selectedCountries.Select(c => new CountryValueDto
                        {
                            CountryID = c.CountryID,
                            CountryName = c.CountryName,
                            Value = values[c.CountryID].First(),
                            AiValue = values[c.CountryID].Last()
                        }).ToList(),
                        PeerCountryScore = peerCountryScore // You can rename property if needed
                    });
                }

                // Append Peer Country Score series
                response.Series.Add(peerSeries);

                return ResultResponseDto<CompareCountryResponseDto>.Success(response);
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error occurred in CompareCountries", ex);
                return ResultResponseDto<CompareCountryResponseDto>.Failure(new List<string> { "An error occurred while comparing countries." });
            }
        }

        public async Task<ResultResponseDto<GetMutiplekpiLayerResultsDto>> GetMutiplekpiLayerResults(
            GetMutiplekpiLayerRequestDto request,
            int userId,
            UserRole role,
            TieredAccessPlan userPlan = TieredAccessPlan.Pending)
        {
            try
            {
                var year = request.Year;
                var startDate = new DateTime(year, 1, 1);
                var endDate = startDate.AddYears(1);

                if (role == UserRole.CountryUser)
                {
                    var validCountryIDs = await _context.PublicUserCountryMappings
                        .Where(x =>
                            x.IsActive &&
                            x.UserID == userId)
                        .Select(x => x.CountryID)
                        .ToListAsync();

                    bool hasInvalidCountry = request.CountryIDs
                        .Any(countryId => !validCountryIDs.Contains(countryId));

                    if (hasInvalidCountry)
                    {
                        return ResultResponseDto<GetMutiplekpiLayerResultsDto>
                            .Failure(new List<string> { "You are not authorized to access one or more selected countries." });
                    }
                }


                var query = _context.AnalyticalLayerResults
                    .AsNoTracking()
                    .Where(x =>
                        request.CountryIDs.Contains(x.CountryID) &&
                        x.LayerID == request.LayerID &&
                        (
                            (x.LastUpdated >= startDate && x.LastUpdated < endDate) ||
                            (x.AiLastUpdated >= startDate && x.AiLastUpdated < endDate)
                        ));

                var response = await query
                    .GroupBy(x => x.LayerID)
                    .Select(g => new GetMutiplekpiLayerResultsDto
                    {
                        LayerID = g.Key,

                        LayerCode = g.Select(x => x.AnalyticalLayer.LayerCode).FirstOrDefault()?? string.Empty,
                        LayerName = g.Select(x => x.AnalyticalLayer.LayerName).FirstOrDefault() ?? string.Empty,
                        Purpose = g.Select(x => x.AnalyticalLayer.Purpose).FirstOrDefault() ?? string.Empty,
                        CalText1 = g.Select(x => x.AnalyticalLayer.CalText1).FirstOrDefault(),
                        CalText2 = g.Select(x => x.AnalyticalLayer.CalText2).FirstOrDefault(),
                        CalText3 = g.Select(x => x.AnalyticalLayer.CalText3).FirstOrDefault(),
                        CalText4 = g.Select(x => x.AnalyticalLayer.CalText4).FirstOrDefault(),
                        CalText5 = g.Select(x => x.AnalyticalLayer.CalText5).FirstOrDefault(),
                        Definition = g.Select(x => x.AnalyticalLayer.Definition).FirstOrDefault(),

                        FiveLevelInterpretations = g.First().AnalyticalLayer.FiveLevelInterpretations,

                        countries = g.Select(x => new MutipleCountrieskpiLayerResults
                        {
                            CountryID = x.CountryID,
                            InterpretationID = x.InterpretationID,
                            NormalizeValue = x.NormalizeValue,
                            CalValue1 = x.CalValue1,
                            CalValue2 = x.CalValue2,
                            CalValue3 = x.CalValue3,
                            CalValue4 = x.CalValue4,
                            CalValue5 = x.CalValue5,
                            LastUpdated = x.LastUpdated,

                            AiInterpretationID = x.AiInterpretationID,
                            AiNormalizeValue = x.AiNormalizeValue,
                            AiCalValue1 = x.AiCalValue1,
                            AiCalValue2 = x.AiCalValue2,
                            AiCalValue3 = x.AiCalValue3,
                            AiCalValue4 = x.AiCalValue4,
                            AiCalValue5 = x.AiCalValue5,

                            AiLastUpdated = x.AiLastUpdated,
                            Country = x.Country
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                return ResultResponseDto<GetMutiplekpiLayerResultsDto>
                    .Success(response ?? new GetMutiplekpiLayerResultsDto());
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error occurred in GetMutiplekpiLayerResults", ex);

                return ResultResponseDto<GetMutiplekpiLayerResultsDto>
                    .Failure(new List<string> { "An error occurred." });
            }
        }
        
        public async Task<Tuple<string, byte[]>> ExportCompareCountries(CompareKpiCountryRequest c, int userId, UserRole role)
        {
            try
            {
                var payload = new CompareCountryRequestDto
                {
                    Countries = c.Countries,  
                    UpdatedAt = c.UpdatedAt
                };

                var result = await CompareCountries(payload, userId, role,false);
                var data = result.Result;

                if (data == null || data.TableData == null || !data.TableData.Any())
                {
                    return new Tuple<string, byte[]>("Country_Kpis_Comparison.xlsx", Array.Empty<byte>());
                }

                using (var workbook = new XLWorkbook())
                {
                    var ws = workbook.Worksheets.Add("Country Comparison");

                    // =========================
                    // 📊 DYNAMIC HEADER SETUP
                    // =========================
                    var countries = data.TableData.First().CountryValues;
                    int totalCols = 2 + (countries.Count * 2);

                    // =========================
                    // 🎯 REPORT HEADER (TOP)
                    // =========================
                    ws.Range(1, 1, 1, totalCols).Merge().Value = "Key Performance Indicator Report";
                    ws.Range(2, 1, 2, totalCols).Merge().Value = $"Report Year: {DateTime.Now.Year}";
                    ws.Range(3, 1, 3, totalCols).Merge().Value = $"Generated On: {DateTime.Now:dd-MMM-yyyy HH:mm}";

                    var titleRange = ws.Range(1, 1, 3, totalCols);
                    titleRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#2F7D6D");
                    titleRange.Style.Font.FontColor = XLColor.White;
                    titleRange.Style.Font.Bold = true;
                    titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    titleRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    ws.Row(1).Height = 28;
                    ws.Row(2).Height = 22;
                    ws.Row(3).Height = 22;

                    // =========================
                    // 📊 MULTI-ROW TABLE HEADER
                    // =========================
                    int row = 5;
                    int col = 1;

                    // KPI Name
                    ws.Range(row, col, row + 1, col).Merge().Value = "KPI Name";
                    col++;

                    // Purpose
                    ws.Range(row, col, row + 1, col).Merge().Value = "Purpose";
                    col++;

                    // Dynamic Countries
                    foreach (var country in countries)
                    {
                        int startCol = col;

                        // Country Name (merged)
                        ws.Range(row, startCol, row, startCol + 1).Merge().Value = country.CountryName;

                        // Sub headers
                        ws.Cell(row + 1, startCol).Value = "Evaluation";
                        ws.Cell(row + 1, startCol + 1).Value = "AI";

                        col += 2;
                    }

                    // Style header (both rows)
                    var headerRange = ws.Range(row, 1, row + 1, totalCols);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Font.FontColor = XLColor.White;
                    headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#2F7D6D");
                    headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // =========================
                    // 📄 DATA ROWS
                    // =========================
                    row += 2;
                    int startDataRow = row;

                    foreach (var kpi in data.TableData)
                    {
                        col = 1;

                        ws.Cell(row, col++).Value = $"{kpi.LayerName} ({kpi.LayerCode})";

                        var cleanPurpose = kpi.Definition ??"";
                        var purposeCell = ws.Cell(row, col++);
                        purposeCell.Value = string.IsNullOrEmpty(cleanPurpose) ? "NA" : cleanPurpose;

                        if (!string.IsNullOrEmpty(cleanPurpose))
                        {
                            var comment = purposeCell.GetComment();
                            comment.AddText(cleanPurpose);
                            comment.Visible = false;
                        }

                        foreach (var country in kpi.CountryValues)
                        {
                            ws.Cell(row, col++).Value = country.Value;
                            ws.Cell(row, col++).Value = country.AiValue;
                        }

                        row++;
                    }

                    int endDataRow = row - 1;

                    // =========================
                    // 🎨 STYLING
                    // =========================

                    // Column widths
                    ws.Column(1).Width = 70;
                    ws.Column(2).Width = 55;

                    for (int i = 3; i <= totalCols; i++)
                    {
                        ws.Column(i).Width = 18;
                    }

                    // Wrap text
                    ws.Column(2).Style.Alignment.WrapText = true;
                    ws.Column(2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;

                    // Center numbers
                    ws.Columns(3, totalCols).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Auto height
                    ws.Rows().AdjustToContents();

                    // Freeze (after 2 header rows)
                    ws.SheetView.FreezeRows(6);

                    // Borders
                    var dataRange = ws.Range(5, 1, endDataRow, totalCols);
                    dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                    // Zebra rows
                    for (int i = startDataRow; i <= endDataRow; i++)
                    {
                        if (i % 2 == 0)
                        {
                            ws.Range(i, 1, i, totalCols).Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
                        }
                    }

                    // Auto filter (second header row)
                    ws.Range(6, 1, 6, totalCols).SetAutoFilter();

                    // =========================
                    // 📄 SHEET 2
                    // =========================
                    var ws2 = workbook.Worksheets.Add("KPI Details");

                    int r = 1;

                    ws2.Cell(r, 1).Value = "KPI Name";
                    ws2.Cell(r, 2).Value = "Full Purpose";

                    var header2 = ws2.Range(r, 1, r, 2);
                    header2.Style.Font.Bold = true;
                    header2.Style.Font.FontColor = XLColor.White;
                    header2.Style.Fill.BackgroundColor = XLColor.FromHtml("#2F7D6D");

                    r++;

                    foreach (var kpi in data.TableData)
                    {
                        ws2.Cell(r, 1).Value = $"{kpi.LayerName} ({kpi.LayerCode})";
                        ws2.Cell(r, 2).Value = kpi.Definition ?? "";
                        r++;
                    }

                    ws2.Column(1).Width = 40;
                    ws2.Column(2).Width = 100;
                    ws2.Column(2).Style.Alignment.WrapText = true;

                    ws2.Rows().AdjustToContents();
                    ws2.SheetView.FreezeRows(1);

                    // =========================
                    // 📤 EXPORT
                    // =========================
                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        return new Tuple<string, byte[]>("Country_Comparison.xlsx", stream.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                await _appLogger.LogAsync("Error in ExportCompareCountries", ex);
                return new Tuple<string, byte[]>("", Array.Empty<byte>());
            }
        }
    }
}

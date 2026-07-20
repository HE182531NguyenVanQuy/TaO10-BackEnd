using ClosedXML.Excel;
using TaO10_BackEnd.Common;

namespace TaO10_BackEnd.Helpers;

public class ExamExcelImportRow
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int QuestionsCount { get; set; }
    public int DurationTime { get; set; }
    public string Level { get; set; } = string.Empty;
    public int Year { get; set; }
    public string ExamType { get; set; } = string.Empty;
    public int ViewsCount { get; set; }
    public int AttemptsCount { get; set; }
    public int? QuestionNumber { get; set; }
    public string Section { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public string OptionA { get; set; } = string.Empty;
    public string OptionB { get; set; } = string.Empty;
    public string OptionC { get; set; } = string.Empty;
    public string OptionD { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public decimal Points { get; set; }
}

public static class ExamExcelParser
{
    private static readonly string[] RequiredHeaders =
    [
        "Title", "Description", "QuestionsCount", "DurationTime", "Level", "Year", "ExamType",
        "ViewsCount", "AttemptsCount", "QuestionNumber", "Section", "QuestionText",
        "OptionA", "OptionB", "OptionC", "OptionD", "CorrectAnswer", "Explanation", "Points"
    ];

    public static List<ExamExcelImportRow> Parse(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault(w =>
            w.Name.Equals("Import", StringComparison.OrdinalIgnoreCase))
            ?? workbook.Worksheets.First();

        var headerRow = worksheet.Row(1);
        var columnMap = BuildColumnMap(headerRow);
        ValidateHeaders(columnMap);

        var rows = new List<ExamExcelImportRow>();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            var row = worksheet.Row(rowNumber);
            var title = GetCellString(row, columnMap, "Title");
            if (string.IsNullOrWhiteSpace(title))
                continue;

            var questionNumber = GetCellNullableInt(row, columnMap, "QuestionNumber");
            var rawSection = GetCellString(row, columnMap, "Section");

            rows.Add(new ExamExcelImportRow
            {
                Title = title.Trim(),
                Description = GetCellString(row, columnMap, "Description"),
                QuestionsCount = GetCellInt(row, columnMap, "QuestionsCount"),
                DurationTime = GetCellInt(row, columnMap, "DurationTime"),
                Level = GetCellString(row, columnMap, "Level"),
                Year = GetCellInt(row, columnMap, "Year"),
                ExamType = GetCellString(row, columnMap, "ExamType"),
                ViewsCount = GetCellInt(row, columnMap, "ViewsCount"),
                AttemptsCount = GetCellInt(row, columnMap, "AttemptsCount"),
                QuestionNumber = questionNumber,
                Section = QuestionTypeConstants.Normalize(rawSection, questionNumber),
                QuestionText = GetCellString(row, columnMap, "QuestionText"),
                OptionA = GetCellString(row, columnMap, "OptionA"),
                OptionB = GetCellString(row, columnMap, "OptionB"),
                OptionC = GetCellString(row, columnMap, "OptionC"),
                OptionD = GetCellString(row, columnMap, "OptionD"),
                CorrectAnswer = GetCellString(row, columnMap, "CorrectAnswer"),
                Explanation = GetCellString(row, columnMap, "Explanation"),
                Points = GetCellDecimal(row, columnMap, "Points")
            });
        }

        if (rows.Count == 0)
            throw new InvalidOperationException("File Excel không có dữ liệu hợp lệ.");

        return rows;
    }

    private static Dictionary<string, int> BuildColumnMap(IXLRow headerRow)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in headerRow.CellsUsed())
        {
            var header = cell.GetString().Trim();
            if (!string.IsNullOrEmpty(header))
                map[header] = cell.Address.ColumnNumber;
        }
        return map;
    }

    private static void ValidateHeaders(Dictionary<string, int> columnMap)
    {
        var missing = RequiredHeaders.Where(h => !columnMap.ContainsKey(h)).ToList();
        if (missing.Count > 0)
            throw new InvalidOperationException($"Thiếu cột bắt buộc: {string.Join(", ", missing)}");
    }

    private static string GetCellString(IXLRow row, Dictionary<string, int> columnMap, string header)
    {
        if (!columnMap.TryGetValue(header, out var col))
            return string.Empty;

        return row.Cell(col).GetString().Trim();
    }

    private static int GetCellInt(IXLRow row, Dictionary<string, int> columnMap, string header)
    {
        if (!columnMap.TryGetValue(header, out var col))
            return 0;

        var cell = row.Cell(col);
        if (cell.TryGetValue(out int intValue))
            return intValue;

        if (cell.TryGetValue(out double doubleValue))
            return (int)doubleValue;

        return int.TryParse(cell.GetString().Trim(), out var parsed) ? parsed : 0;
    }

    private static int? GetCellNullableInt(IXLRow row, Dictionary<string, int> columnMap, string header)
    {
        if (!columnMap.TryGetValue(header, out var col))
            return null;

        var cell = row.Cell(col);
        if (cell.IsEmpty())
            return null;

        if (cell.TryGetValue(out int intValue))
            return intValue;

        if (cell.TryGetValue(out double doubleValue))
            return (int)doubleValue;

        var text = cell.GetString().Trim();
        return int.TryParse(text, out var parsed) ? parsed : null;
    }

    private static decimal GetCellDecimal(IXLRow row, Dictionary<string, int> columnMap, string header)
    {
        if (!columnMap.TryGetValue(header, out var col))
            return 0;

        var cell = row.Cell(col);
        if (cell.TryGetValue(out decimal decimalValue))
            return decimalValue;

        if (cell.TryGetValue(out double doubleValue))
            return (decimal)doubleValue;

        return decimal.TryParse(cell.GetString().Trim(), out var parsed) ? parsed : 0;
    }
}

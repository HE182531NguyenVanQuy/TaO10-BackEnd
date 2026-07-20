using Microsoft.EntityFrameworkCore;
using TaO10_BackEnd.Common;
using TaO10_BackEnd.DTOs.Exams;
using TaO10_BackEnd.Helpers;
using TaO10_BackEnd.Models;

namespace TaO10_BackEnd.Services;

public class ExamImportService : IExamImportService
{
    private const string NotApplicable = "N/A";

    private static readonly IReadOnlyList<(string Code, string Name)> PackageImportChain =
    [
        ("free", "Gói Dùng Thử"),
        ("1Month", "Gói Cấp Tốc"),
        ("3Month", "Gói Chuyên Sâu"),
        ("6Month", "Gói Nâng Cao"),
        ("12Month", "Gói Premium")
    ];

    private readonly AppDbContext _context;
    private readonly ILogger<ExamImportService> _logger;

    public ExamImportService(AppDbContext context, ILogger<ExamImportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ExamImportResultDto> ImportFromExcelAsync(Stream excelStream, CancellationToken cancellationToken = default)
    {
        return await ImportFromExcelAsync(excelStream, "free", cancellationToken);
    }

    public async Task<ExamImportResultDto> ImportFromExcelAsync(
        Stream excelStream,
        string packageCode,
        CancellationToken cancellationToken = default)
    {
        var rows = ExamExcelParser.Parse(excelStream);
        var targetPackageConfigs = GetTargetPackageConfigs(packageCode);
        var result = new ExamImportResultDto
        {
            PackageName = targetPackageConfigs[0].Name,
            PackageNames = targetPackageConfigs.Select(package => package.Name).ToList(),
            PackageCodes = targetPackageConfigs.Select(package => package.Code).ToList()
        };

        var examStatus = await _context.Statuses
            .FirstOrDefaultAsync(s =>
                s.EntityType == AppStatusCodes.EntityTypes.Exam &&
                s.Code == AppStatusCodes.Exams.Active, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy status ACTIVE cho Exam.");

        var questionStatus = await _context.Statuses
            .FirstOrDefaultAsync(s =>
                s.EntityType == AppStatusCodes.EntityTypes.Question &&
                s.Code == AppStatusCodes.Questions.Active, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy status ACTIVE cho Question.");

        var packageNames = targetPackageConfigs.Select(package => package.Name).ToList();
        var targetPackages = await _context.Packages
            .Include(package => package.PackageExams)
            .Where(package => packageNames.Contains(package.Name))
            .ToListAsync(cancellationToken);

        var missingPackages = packageNames
            .Except(targetPackages.Select(package => package.Name), StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (missingPackages.Count > 0)
            throw new InvalidOperationException($"Không tìm thấy gói: {string.Join(", ", missingPackages)}.");

        var linkedExamIdsByPackage = targetPackages.ToDictionary(
            package => package.PackageId,
            package => package.PackageExams.Select(packageExam => packageExam.ExamId).ToHashSet());

        var groupedExams = rows
            .GroupBy(row => row.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var examGroup in groupedExams)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var firstRow = examGroup.First();
                var existingExam = await _context.Exams
                    .FirstOrDefaultAsync(exam => exam.Title == examGroup.Key, cancellationToken);

                Exam exam;
                if (existingExam != null)
                {
                    exam = existingExam;
                    result.ExamsSkipped++;
                    result.SkippedExamTitles.Add(examGroup.Key);
                }
                else
                {
                    var gradableCount = examGroup.Count(row => !IsPassageRow(row));

                    exam = new Exam
                    {
                        ExamId = Guid.NewGuid(),
                        Title = firstRow.Title,
                        Description = firstRow.Description,
                        QuestionsCount = gradableCount,
                        DurationTime = firstRow.DurationTime > 0 ? firstRow.DurationTime : 60,
                        Level = NormalizeLevel(firstRow.Level),
                        Year = firstRow.Year > 0 ? firstRow.Year : DateTime.UtcNow.Year,
                        ExamType = Truncate(firstRow.ExamType, 100),
                        ViewsCount = firstRow.ViewsCount,
                        AttemptsCount = firstRow.AttemptsCount,
                        StatusId = examStatus.StatusId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Exams.Add(exam);
                    result.ExamsCreated++;
                    result.CreatedExamTitles.Add(examGroup.Key);

                    var rowIndex = 0;
                    foreach (var row in examGroup)
                    {
                        var question = MapQuestion(row, exam.ExamId, questionStatus.StatusId, rowIndex++);
                        _context.Questions.Add(question);
                        result.QuestionsCreated++;
                    }
                }

                foreach (var package in targetPackages)
                {
                    var linkedExamIds = linkedExamIdsByPackage[package.PackageId];
                    if (linkedExamIds.Contains(exam.ExamId))
                        continue;

                    _context.PackageExams.Add(new PackageExam
                    {
                        PackageExamId = Guid.NewGuid(),
                        PackageId = package.PackageId,
                        ExamId = exam.ExamId,
                        CreatedAt = DateTime.UtcNow
                    });
                    linkedExamIds.Add(exam.ExamId);
                    result.PackageLinksCreated++;
                }
            }

            foreach (var package in targetPackages)
                package.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Imported exams from Excel. Created: {Created}, Skipped: {Skipped}, Questions: {Questions}, PackageLinks: {Links}, PackageCode: {PackageCode}",
                result.ExamsCreated,
                result.ExamsSkipped,
                result.QuestionsCreated,
                result.PackageLinksCreated,
                packageCode);

            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static Question MapQuestion(ExamExcelImportRow row, Guid examId, Guid questionStatusId, int rowIndex)
    {
        var isPassage = IsPassageRow(row);

        return new Question
        {
            QuestionId = Guid.NewGuid(),
            ExamId = examId,
            QuestionNumber = isPassage ? 0 : row.QuestionNumber ?? 0,
            Section = QuestionTypeConstants.Normalize(row.Section, row.QuestionNumber),
            QuestionText = row.QuestionText,
            OptionA = isPassage ? null : NullIfNa(row.OptionA),
            OptionB = isPassage ? null : NullIfNa(row.OptionB),
            OptionC = isPassage ? null : NullIfNa(row.OptionC),
            OptionD = isPassage ? null : NullIfNa(row.OptionD),
            CorrectAnswer = isPassage ? null : Truncate(NullIfNa(row.CorrectAnswer), 10),
            Explanation = string.IsNullOrWhiteSpace(row.Explanation) ? "Không có giải thích." : row.Explanation,
            Points = row.Points,
            StatusId = questionStatusId,
            CreatedAt = DateTime.UtcNow.AddTicks(rowIndex)
        };
    }

    private static bool IsPassageRow(ExamExcelImportRow row) =>
        row.QuestionNumber == null ||
        IsLiteralNotApplicable(row.CorrectAnswer) ||
        AllOptionsAreNotApplicable(row);

    private static bool AllOptionsAreNotApplicable(ExamExcelImportRow row) =>
        IsNotApplicable(row.OptionA) &&
        IsNotApplicable(row.OptionB) &&
        IsNotApplicable(row.OptionC) &&
        IsNotApplicable(row.OptionD);

    private static string? NullIfNa(string? value) =>
        IsNotApplicable(value) ? null : value!.Trim();

    private static bool IsNotApplicable(string? value) =>
        string.IsNullOrWhiteSpace(value) ||
        string.Equals(value.Trim(), NotApplicable, StringComparison.OrdinalIgnoreCase);

    private static bool IsLiteralNotApplicable(string? value) =>
        string.Equals(value?.Trim(), NotApplicable, StringComparison.OrdinalIgnoreCase);

    private static string? NormalizeLevel(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        return normalized switch
        {
            "easy" => "easy",
            "medium" => "medium",
            "hard" => "hard",
            _ => "medium"
        };
    }

    private static List<(string Code, string Name)> GetTargetPackageConfigs(string packageCode)
    {
        var startIndex = PackageImportChain
            .Select((package, index) => new { package, index })
            .FirstOrDefault(item => string.Equals(item.package.Code, packageCode, StringComparison.OrdinalIgnoreCase))
            ?.index;

        if (startIndex == null)
        {
            var supportedCodes = string.Join(", ", PackageImportChain.Select(package => package.Code));
            throw new InvalidOperationException($"Code gói không hợp lệ. Hỗ trợ: {supportedCodes}.");
        }

        return PackageImportChain.Skip(startIndex.Value).ToList();
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}

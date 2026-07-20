using System.Globalization;
using System.Text;

namespace TaO10_BackEnd.Common;

public static class QuestionTypeConstants
{
    public const string Pronunciation = "Phát âm";
    public const string Stress = "Trọng âm";
    public const string Grammar = "Ngữ pháp";
    public const string Vocabulary = "Từ vựng";
    public const string MultipleChoice = "Chọn đáp án đúng";
    public const string Communication = "Giao tiếp";
    public const string Synonym = "Từ đồng nghĩa";
    public const string Antonym = "Từ trái nghĩa";
    public const string SynonymAntonym = "Từ đồng nghĩa / trái nghĩa";
    public const string Cloze = "Đọc hiểu - điền từ";
    public const string SentenceInsertion = "Điền câu vào đoạn văn";
    public const string Reading = "Đọc hiểu";
    public const string SignNotice = "Biển báo/Thông báo";
    public const string Listening = "Nghe hiểu";
    public const string Writing = "Viết";
    public const string Rewrite = "Viết lại câu (gần nghĩa)";
    public const string RewriteWithGivenWords = "Viết lại câu (từ cho sẵn)";
    public const string ArrangeCompleteParagraph = "Sắp xếp/hoàn thành đoạn văn";
    public const string ErrorCorrection = "Tìm lỗi sai";
    public const string Mixed = "Đề tổng hợp";
    public const string Other = "Khác";

    public static readonly IReadOnlyList<string> All =
    [
        Pronunciation,
        Stress,
        Grammar,
        Vocabulary,
        MultipleChoice,
        Communication,
        Synonym,
        Antonym,
        SynonymAntonym,
        Cloze,
        SentenceInsertion,
        Reading,
        SignNotice,
        Listening,
        Writing,
        Rewrite,
        RewriteWithGivenWords,
        ArrangeCompleteParagraph,
        ErrorCorrection,
        Mixed,
        Other
    ];

    private static readonly IReadOnlyDictionary<string, string> Aliases = new Dictionary<string, string>
    {
        ["pronunciation"] = Pronunciation,
        ["phonetics"] = Pronunciation,
        ["phat am"] = Pronunciation,
        ["stress"] = Stress,
        ["trong am"] = Stress,
        ["grammar"] = Grammar,
        ["ngu phap"] = Grammar,
        ["vocabulary"] = Vocabulary,
        ["vocab"] = Vocabulary,
        ["tu vung"] = Vocabulary,
        ["chon dap an dung"] = MultipleChoice,
        ["multiple choice"] = MultipleChoice,
        ["choose the best answer"] = MultipleChoice,
        ["communication"] = Communication,
        ["communicative"] = Communication,
        ["giao tiep"] = Communication,
        ["synonym"] = Synonym,
        ["tu dong nghia"] = Synonym,
        ["antonym"] = Antonym,
        ["tu trai nghia"] = Antonym,
        ["synonym antonym"] = SynonymAntonym,
        ["tu dong nghia trai nghia"] = SynonymAntonym,
        ["cloze"] = Cloze,
        ["cloze test"] = Cloze,
        ["doc hieu dien tu"] = Cloze,
        ["dien tu vao doan van"] = Cloze,
        ["dien cau vao doan van"] = SentenceInsertion,
        ["sentence insertion"] = SentenceInsertion,
        ["reading"] = Reading,
        ["reading comprehension"] = Reading,
        ["doc hieu"] = Reading,
        ["bai doc"] = Reading,
        ["passage"] = Reading,
        ["bien bao thong bao"] = SignNotice,
        ["sign notice"] = SignNotice,
        ["notice"] = SignNotice,
        ["listening"] = Listening,
        ["nghe"] = Listening,
        ["nghe hieu"] = Listening,
        ["writing"] = Writing,
        ["viet"] = Writing,
        ["rewrite"] = Rewrite,
        ["sentence transformation"] = Rewrite,
        ["viet lai cau"] = Rewrite,
        ["viet lai cau gan nghia"] = Rewrite,
        ["viet lai cau tu cho san"] = RewriteWithGivenWords,
        ["given words"] = RewriteWithGivenWords,
        ["sap xep hoan thanh doan van"] = ArrangeCompleteParagraph,
        ["arrange complete paragraph"] = ArrangeCompleteParagraph,
        ["error correction"] = ErrorCorrection,
        ["find mistake"] = ErrorCorrection,
        ["tim loi sai"] = ErrorCorrection,
        ["mixed"] = Mixed,
        ["general"] = Mixed,
        ["de tong hop"] = Mixed,
        ["tong hop"] = Mixed,
        ["other"] = Other,
        ["khac"] = Other
    };

    public static string Normalize(string? value, int? questionNumber = null)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return InferFromQuestionNumber(questionNumber);

        var knownType = All.FirstOrDefault(type =>
            string.Equals(type, trimmed, StringComparison.OrdinalIgnoreCase));
        if (knownType != null)
            return knownType;

        var key = ToLookupKey(trimmed);
        if (Aliases.TryGetValue(key, out var exact))
            return exact;

        foreach (var alias in Aliases)
        {
            if (key.Contains(alias.Key, StringComparison.OrdinalIgnoreCase))
                return alias.Value;
        }

        return InferFromQuestionNumber(questionNumber, trimmed);
    }

    public static string InferFromQuestionNumber(int? questionNumber, string? fallback = null)
    {
        if (!questionNumber.HasValue || questionNumber <= 0)
            return string.IsNullOrWhiteSpace(fallback) ? Other : fallback.Trim();

        return questionNumber.Value switch
        {
            >= 1 and <= 2 => Pronunciation,
            >= 3 and <= 4 => Stress,
            >= 5 and <= 18 => MultipleChoice,
            >= 19 and <= 20 => Communication,
            >= 21 and <= 24 => SynonymAntonym,
            >= 25 and <= 30 => Cloze,
            >= 31 and <= 34 => Reading,
            >= 35 and <= 40 => Rewrite,
            _ => string.IsNullOrWhiteSpace(fallback) ? Other : fallback.Trim()
        };
    }

    private static string ToLookupKey(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;

            builder.Append(ch == 'đ' || ch == 'Đ' ? 'd' : ch);
        }

        var ascii = builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        var clean = new StringBuilder(ascii.Length);

        foreach (var ch in ascii)
            clean.Append(char.IsLetterOrDigit(ch) ? ch : ' ');

        return string.Join(' ', clean.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}

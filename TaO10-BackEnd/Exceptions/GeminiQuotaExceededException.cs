namespace TaO10_BackEnd.Exceptions;

public class OpenRouterQuotaExceededException : Exception
{
    public string ErrorCode { get; }

    public OpenRouterQuotaExceededException(
        string message = "OpenRouter đang giới hạn lượt gọi, vui lòng thử lại sau ít phút.",
        string errorCode = "OPENROUTER_QUOTA_EXCEEDED")
        : base(message)
    {
        ErrorCode = errorCode;
    }
}

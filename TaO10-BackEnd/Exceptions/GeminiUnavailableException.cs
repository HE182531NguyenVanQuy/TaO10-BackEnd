namespace TaO10_BackEnd.Exceptions;

public class OpenRouterUnavailableException : Exception
{
    public string ErrorCode { get; }

    public OpenRouterUnavailableException(
        string message = "OpenRouter đang quá tải, vui lòng thử lại sau ít phút.",
        string errorCode = "OPENROUTER_UNAVAILABLE")
        : base(message)
    {
        ErrorCode = errorCode;
    }
}

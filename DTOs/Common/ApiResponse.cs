namespace RAQEEB.DTOs.Common
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }

        public T? Data { get; set; }

        public string Message { get; set; } = string.Empty;

        public string? ErrorCode { get; set; }

        public List<ApiError> Errors { get; set; } = new();

        public static ApiResponse<T> SuccessResponse(
            T data,
            string message)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data,
                Message = message
            };
        }

        public static ApiResponse<T> FailureResponse(
            string errorCode,
            string message,
            List<ApiError>? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Data = default,
                ErrorCode = errorCode,
                Message = message,
                Errors = errors ?? new List<ApiError>()
            };
        }
    }

    public class ApiError
    {
        public string Field { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
    }
}
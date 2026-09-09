namespace ProcurementSystem.Core.DTOs
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }

        public static ApiResponse<T> SuccessResponse(T data, string message = "Thành công")
        {
            return new ApiResponse<T> { Success = true, Message = message, Data = data };
        }

        public static ApiResponse<T> FailResponse(string message, List<string>? errors = null)
        {
            return new ApiResponse<T> { Success = false, Message = message, Errors = errors };
        }

        public static ApiResponse<T> Ok(T data, string message = "Thành công")
        {
            return SuccessResponse(data, message);
        }

        public static ApiResponse<T> Fail(string message, List<string>? errors = null)
        {
            return FailResponse(message, errors);
        }

        public static ApiResponse<T> Fail(List<string> errors, string message = "Yêu cầu không hợp lệ")
        {
            return new ApiResponse<T> { Success = false, Message = message, Errors = errors };
        }
    }
}

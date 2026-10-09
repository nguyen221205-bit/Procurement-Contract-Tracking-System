namespace ProcurementSystem.Core.DTOs
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }

        public int StatusCode { get; set; } = 200;

        public static ApiResponse<T> SuccessResponse(T data, string message = "Thành công", int statusCode = 200)
        {
            return new ApiResponse<T> { Success = true, Message = message, Data = data, StatusCode = statusCode };
        }

        public static ApiResponse<T> FailResponse(string message, List<string>? errors = null, int statusCode = 400)
        {
            return new ApiResponse<T> { Success = false, Message = message, Errors = errors, StatusCode = statusCode };
        }

        public static ApiResponse<T> Ok(T data, string message = "Thành công")
        {
            return SuccessResponse(data, message, 200);
        }

        public static ApiResponse<T> Fail(string message, List<string>? errors = null, int statusCode = 400)
        {
            return FailResponse(message, errors, statusCode);
        }

        public static ApiResponse<T> Fail(List<string> errors, string message = "Yêu cầu không hợp lệ", int statusCode = 400)
        {
            return new ApiResponse<T> { Success = false, Message = message, Errors = errors, StatusCode = statusCode };
        }

        public static ApiResponse<T> Forbidden(string message = "Bạn không có quyền thực hiện thao tác này.")
        {
            return new ApiResponse<T> { Success = false, Message = message, StatusCode = 403 };
        }
    }
}

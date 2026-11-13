using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.DTOs.BidSubmission;
using ProcurementSystem.Core.DTOs.Common;

namespace ProcurementSystem.API.Extensions
{
    /// <summary>
    /// Các phương thức mở rộng giúp ánh xạ nhất quán kết quả từ tầng Service (ApiResponse / FileDownloadResult)
    /// sang HTTP ActionResult tại tầng Controller, triệt tiêu các chuỗi if-else lặp lại.
    /// </summary>
    public static class ControllerResultExtensions
    {
        public static ActionResult<ApiResponse<T>> ToActionResult<T>(
            this ApiResponse<T> result,
            ControllerBase controller,
            int successStatusCode = StatusCodes.Status200OK,
            int defaultFailureStatusCode = StatusCodes.Status400BadRequest)
        {
            if (result.Success)
            {
                return successStatusCode == StatusCodes.Status200OK
                    ? controller.Ok(result)
                    : controller.StatusCode(successStatusCode, result);
            }

            var errorStatusCode = ResolveErrorStatusCode(result, defaultFailureStatusCode);
            return errorStatusCode switch
            {
                StatusCodes.Status404NotFound => controller.NotFound(result),
                StatusCodes.Status400BadRequest => controller.BadRequest(result),
                StatusCodes.Status401Unauthorized => controller.Unauthorized(result),
                _ => controller.StatusCode(errorStatusCode, result)
            };
        }

        public static IActionResult ToFileActionResult(
            this FileDownloadResult? fileData,
            ControllerBase controller,
            string notFoundMessage = "Không tìm thấy tệp tài liệu trên hệ thống lưu trữ.")
        {
            if (fileData == null)
            {
                return controller.NotFound(ApiResponse<string>.NotFound(notFoundMessage));
            }

            if (fileData.IsRedirect)
            {
                return controller.Redirect(fileData.RedirectUrl!);
            }

            if (fileData.IsStream)
            {
                return controller.File(fileData.FileStream!, fileData.ContentType, fileData.FileName);
            }

            if (string.IsNullOrEmpty(fileData.PhysicalPath) || !System.IO.File.Exists(fileData.PhysicalPath))
            {
                return controller.NotFound(ApiResponse<string>.NotFound(notFoundMessage));
            }

            return controller.PhysicalFile(fileData.PhysicalPath!, fileData.ContentType, fileData.FileName);
        }

        public static IActionResult ToFileActionResult(
            this ApiResponse<FileDownloadResult> result,
            ControllerBase controller,
            string fallbackMessage = "Không tìm thấy tệp tài liệu.")
        {
            if (!result.Success || result.Data == null)
            {
                var statusCode = result.StatusCode >= 400 && result.StatusCode != StatusCodes.Status400BadRequest
                    ? result.StatusCode
                    : StatusCodes.Status404NotFound;

                var payload = ApiResponse<string>.Fail(
                    !string.IsNullOrWhiteSpace(result.Message) ? result.Message : fallbackMessage,
                    statusCode: statusCode);

                return statusCode == StatusCodes.Status404NotFound
                    ? controller.NotFound(payload)
                    : controller.StatusCode(statusCode, payload);
            }

            return result.Data.ToFileActionResult(controller, fallbackMessage);
        }

        public static IActionResult ToSubmissionFileActionResult(
            this ApiResponse<SubmissionFileDownloadDto> result,
            ControllerBase controller,
            string fallbackMessage = "Tệp tài liệu không tồn tại trên hệ thống lưu trữ.")
        {
            if (!result.Success || result.Data == null)
            {
                if (result.StatusCode == StatusCodes.Status403Forbidden)
                {
                    return controller.StatusCode(
                        StatusCodes.Status403Forbidden,
                        ApiResponse<string>.Forbidden(result.Message));
                }

                return controller.NotFound(ApiResponse<string>.NotFound(
                    !string.IsNullOrWhiteSpace(result.Message) ? result.Message : fallbackMessage));
            }

            var file = result.Data;
            if (file.IsRedirect)
            {
                return controller.Redirect(file.RedirectUrl!);
            }

            if (file.IsStream)
            {
                return controller.File(file.FileStream!, file.ContentType, file.FileName);
            }

            if (string.IsNullOrEmpty(file.PhysicalPath) || !System.IO.File.Exists(file.PhysicalPath))
            {
                return controller.NotFound(ApiResponse<string>.NotFound(fallbackMessage));
            }

            return controller.PhysicalFile(file.PhysicalPath!, file.ContentType, file.FileName);
        }

        private static int ResolveErrorStatusCode<T>(ApiResponse<T> result, int defaultFailureStatusCode)
        {
            if (result.StatusCode >= 400 && result.StatusCode != StatusCodes.Status400BadRequest)
            {
                return result.StatusCode;
            }

            if (!string.IsNullOrWhiteSpace(result.Message) &&
                result.Message.Contains("Không tìm thấy", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCodes.Status404NotFound;
            }

            return defaultFailureStatusCode;
        }
    }
}

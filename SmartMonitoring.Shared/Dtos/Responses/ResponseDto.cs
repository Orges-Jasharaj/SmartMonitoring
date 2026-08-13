using System.Collections.Generic;

namespace SmartMonitoring.Shared.Dtos.Responses
{
    public class ResponseDto<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public List<ApiError>? Errors { get; set; }

        public ResponseDto()
        {
        }

        public ResponseDto(bool success, string? message, T? data = default, List<ApiError>? errors = null)
        {
            Success = success;
            Message = message;
            Data = data;
            Errors = errors ?? new List<ApiError>();
        }

        public static ResponseDto<T> SuccessResponse(T data, string? message = null)
        {
            return new ResponseDto<T>(true, message, data);
        }

        public static ResponseDto<T> Failure(string? message = null, List<ApiError>? errors = null)
        {
            return new ResponseDto<T>(false, message, default, errors);
        }
    }

    public class ApiError
    {
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
    }
}

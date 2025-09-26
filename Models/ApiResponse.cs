using System;
using System.Collections.Generic;

namespace SmkcApi.Models
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public string ErrorCode { get; set; }
        public DateTime Timestamp { get; set; }
        public string RequestId { get; set; }

        public ApiResponse()
        {
            Timestamp = DateTime.UtcNow;
            RequestId = Guid.NewGuid().ToString();
        }

        public static ApiResponse<T> CreateSuccess(T data, string message = "Success")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static ApiResponse<T> CreateError(string message, string errorCode = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                ErrorCode = errorCode
            };
        }
    }

    public class PagedResult<T>
    {
        public List<T> Data { get; set; }
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }

        public PagedResult()
        {
            Data = new List<T>();
        }
    }

    public class ErrorDetail
    {
        public string Field { get; set; }
        public string Message { get; set; }
        public string Code { get; set; }
    }

    public class ValidationErrorResponse
    {
        public bool Success { get; set; } = false;
        public string Message { get; set; } = "Validation failed";
        public List<ErrorDetail> Errors { get; set; }
        public DateTime Timestamp { get; set; }
        public string RequestId { get; set; }

        public ValidationErrorResponse()
        {
            Errors = new List<ErrorDetail>();
            Timestamp = DateTime.UtcNow;
            RequestId = Guid.NewGuid().ToString();
        }
    }
}
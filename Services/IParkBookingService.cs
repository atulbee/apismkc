using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmkcApi.Models;

namespace SmkcApi.Services
{
    /// <summary>
    /// Interface for Park Booking Service
    /// Business logic layer for park booking operations
    /// </summary>
    public interface IParkBookingService
    {
        #region Citizen Operations

        /// <summary>
        /// Register a new citizen and send OTP
        /// </summary>
        Task<ApiResponse<CitizenRegistrationResponse>> RegisterCitizenAsync(CitizenRegistrationRequest request);

        /// <summary>
        /// Verify OTP and issue auth token
        /// </summary>
        Task<ApiResponse<OTPVerificationResponse>> VerifyOtpAsync(OTPVerificationRequest request);

        /// <summary>
        /// Resend OTP to citizen
        /// </summary>
        Task<ApiResponse<ResendOTPResponse>> ResendOtpAsync(ResendOTPRequest request);

        #endregion

        #region Slot Operations

        /// <summary>
        /// Get available slots for a date
        /// </summary>
        Task<ApiResponse<AvailableSlotsResponse>> GetAvailableSlotsAsync(string date);

        #endregion

        #region Booking Operations

        /// <summary>
        /// Create a new booking
        /// </summary>
        Task<ApiResponse<BookingCreateResponse>> CreateBookingAsync(BookingRequest request);

        /// <summary>
        /// Get booking details
        /// </summary>
        Task<ApiResponse<BookingDetailsResponse>> GetBookingDetailsAsync(string bookingId);

        /// <summary>
        /// Get all bookings with pagination and filters
        /// </summary>
        Task<ApiResponse<BookingsListResponse>> GetAllBookingsAsync(BookingFilterRequest request);

        /// <summary>
        /// Search booking by token
        /// </summary>
        Task<ApiResponse<BookingDetailsResponse>> SearchBookingByTokenAsync(string token);

        /// <summary>
        /// Verify entry for a booking
        /// </summary>
        Task<ApiResponse<VerifyEntryResponse>> VerifyEntryAsync(string bookingId, VerifyEntryRequest request);

        /// <summary>
        /// Cancel a booking
        /// </summary>
        Task<ApiResponse<CancelBookingResponse>> CancelBookingAsync(string bookingId, CancelBookingRequest request);

        #endregion

        #region Department Operations

        /// <summary>
        /// Department user login
        /// </summary>
        Task<ApiResponse<DepartmentLoginResponse>> DepartmentLoginAsync(DepartmentLoginRequest request);

        /// <summary>
        /// Get dashboard statistics
        /// </summary>
        Task<ApiResponse<DashboardStatsResponse>> GetDashboardStatsAsync(string userId);

        #endregion

        #region Utility Operations

        /// <summary>
        /// Generate booking receipt
        /// </summary>
        Task<ApiResponse<ReceiptResponse>> GenerateReceiptAsync(string bookingId);

        /// <summary>
        /// Generate booking report
        /// </summary>
        Task<ApiResponse<List<dynamic>>> GenerateBookingReportAsync(ReportRequest request);

        /// <summary>
        /// Send SMS notification
        /// </summary>
        Task<ApiResponse<object>> SendSmsAsync(SmsRequest request);

        /// <summary>
        /// Generate QR code
        /// </summary>
        Task<ApiResponse<QrCodeResponse>> GenerateQrCodeAsync(QrCodeRequest request);

        #endregion
    }
}

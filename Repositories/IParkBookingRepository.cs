using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmkcApi.Models;

namespace SmkcApi.Repositories
{
    /// <summary>
    /// Interface for Park Booking Repository
    /// Handles all database operations for park booking system
    /// </summary>
    public interface IParkBookingRepository
    {
        #region Citizen Management

        /// <summary>
        /// Register a new citizen
        /// </summary>
        Task<CitizenRegistrationResponse> RegisterCitizenAsync(string name, string mobile);

        /// <summary>
        /// Verify OTP for citizen
        /// </summary>
        Task<OTPVerificationResponse> VerifyOtpAsync(string citizenId, string mobile, string otp);

        /// <summary>
        /// Resend OTP to citizen
        /// </summary>
        Task<ResendOTPResponse> ResendOtpAsync(string citizenId, string mobile);

        #endregion

        #region Slot Management

        /// <summary>
        /// Get available slots for a specific date
        /// </summary>
        Task<AvailableSlotsResponse> GetAvailableSlotsAsync(string date);

        #endregion

        #region Booking Management

        /// <summary>
        /// Create a new booking
        /// </summary>
        Task<BookingCreateResponse> CreateBookingAsync(string citizenId, string date, string slotId, string slotCode);

        /// <summary>
        /// Get booking details by booking ID
        /// </summary>
        Task<BookingDetailsResponse> GetBookingDetailsAsync(string bookingId);

        /// <summary>
        /// Get paginated list of bookings with filters
        /// </summary>
        Task<BookingsListResponse> GetAllBookingsAsync(int page, int pageSize, string status, string date, 
            string searchToken, string sortBy, string sortOrder);

        /// <summary>
        /// Search booking by token
        /// </summary>
        Task<BookingDetailsResponse> SearchBookingByTokenAsync(string token);

        /// <summary>
        /// Verify entry for a booking
        /// </summary>
        Task<VerifyEntryResponse> VerifyEntryAsync(string bookingId, string token, string verifiedBy, string notes);

        /// <summary>
        /// Cancel a booking
        /// </summary>
        Task<CancelBookingResponse> CancelBookingAsync(string bookingId, string reason, string cancelledBy);

        #endregion

        #region Department Management

        /// <summary>
        /// Authenticate department user
        /// </summary>
        Task<DepartmentLoginResponse> DepartmentLoginAsync(string userId, string password);

        /// <summary>
        /// Get dashboard statistics
        /// </summary>
        Task<DashboardStatsResponse> GetDashboardStatsAsync(string userId);

        #endregion

        #region Utility Functions

        /// <summary>
        /// Generate receipt for booking
        /// </summary>
        Task<ReceiptResponse> GenerateReceiptAsync(string bookingId);

        /// <summary>
        /// Generate booking report
        /// </summary>
        Task<List<dynamic>> GenerateBookingReportAsync(string startDate, string endDate, string reportType);

        /// <summary>
        /// Log SMS in database
        /// </summary>
        Task<string> LogSmsAsync(string mobile, string message, string templateId);

        #endregion
    }
}

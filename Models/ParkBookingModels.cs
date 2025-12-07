using System;
using System.Collections.Generic;

namespace SmkcApi.Models
{
    #region Request Models

    /// <summary>
    /// Citizen registration request
    /// </summary>
    public class CitizenRegistrationRequest
    {
        public string Name { get; set; }
        public string Mobile { get; set; }
    }

    /// <summary>
    /// OTP verification request
    /// </summary>
    public class OTPVerificationRequest
    {
        public string CitizenId { get; set; }
        public string Mobile { get; set; }
        public string Otp { get; set; }
    }

    /// <summary>
    /// Resend OTP request
    /// </summary>
    public class ResendOTPRequest
    {
        public string CitizenId { get; set; }
        public string Mobile { get; set; }
    }

    /// <summary>
    /// Booking creation request
    /// </summary>
    public class BookingRequest
    {
        public string CitizenId { get; set; }
        public string Date { get; set; }
        public string SlotId { get; set; }
        public string SlotCode { get; set; }
    }

    /// <summary>
    /// Department login request
    /// </summary>
    public class DepartmentLoginRequest
    {
        public string UserId { get; set; }
        public string Password { get; set; }
    }

    /// <summary>
    /// Booking filter request
    /// </summary>
    public class BookingFilterRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string Status { get; set; }
        public string Date { get; set; }
        public string SearchToken { get; set; }
        public string SortBy { get; set; } = "bookingTime";
        public string SortOrder { get; set; } = "desc";
    }

    /// <summary>
    /// Verify entry request
    /// </summary>
    public class VerifyEntryRequest
    {
        public string BookingId { get; set; }
        public string Token { get; set; }
        public string VerifiedBy { get; set; }
        public string Notes { get; set; }
    }

    /// <summary>
    /// Cancel booking request
    /// </summary>
    public class CancelBookingRequest
    {
        public string BookingId { get; set; }
        public string Reason { get; set; }
        public string CancelledBy { get; set; }
    }

    /// <summary>
    /// Report request
    /// </summary>
    public class ReportRequest
    {
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string Format { get; set; } = "pdf";
        public string ReportType { get; set; } = "CUSTOM";
    }

    /// <summary>
    /// SMS request
    /// </summary>
    public class SmsRequest
    {
        public string Mobile { get; set; }
        public string Message { get; set; }
        public string TemplateId { get; set; }
    }

    /// <summary>
    /// QR Code generation request
    /// </summary>
    public class QrCodeRequest
    {
        public string Token { get; set; }
        public string BookingId { get; set; }
    }

    #endregion

    #region Response Models

    /// <summary>
    /// Citizen registration response
    /// </summary>
    public class CitizenRegistrationResponse
    {
        public string CitizenId { get; set; }
        public string Name { get; set; }
        public string Mobile { get; set; }
        public DateTime OtpSentAt { get; set; }
        public DateTime OtpExpiresAt { get; set; }
    }

    /// <summary>
    /// OTP verification response
    /// </summary>
    public class OTPVerificationResponse
    {
        public string CitizenId { get; set; }
        public bool IsVerified { get; set; }
        public string AuthToken { get; set; }
        public DateTime TokenExpiry { get; set; }
    }

    /// <summary>
    /// Resend OTP response
    /// </summary>
    public class ResendOTPResponse
    {
        public DateTime OtpSentAt { get; set; }
        public DateTime OtpExpiresAt { get; set; }
        public int AttemptsRemaining { get; set; }
    }

    /// <summary>
    /// Time slot response
    /// </summary>
    public class TimeSlotResponse
    {
        public string SlotId { get; set; }
        public string SlotCode { get; set; }
        public string SlotTime { get; set; }
        public string SlotLabel { get; set; }
        public int TotalCapacity { get; set; }
        public int BookedCount { get; set; }
        public int AvailableSeats { get; set; }
        public bool IsAvailable { get; set; }
    }

    /// <summary>
    /// Available slots response
    /// </summary>
    public class AvailableSlotsResponse
    {
        public string Date { get; set; }
        public List<TimeSlotResponse> Slots { get; set; }

        public AvailableSlotsResponse()
        {
            Slots = new List<TimeSlotResponse>();
        }
    }

    /// <summary>
    /// Booking creation response
    /// </summary>
    public class BookingCreateResponse
    {
        public string BookingId { get; set; }
        public string Token { get; set; }
        public string CitizenName { get; set; }
        public string Mobile { get; set; }
        public string Date { get; set; }
        public string SlotTime { get; set; }
        public string SlotLabel { get; set; }
        public DateTime BookingTime { get; set; }
        public string Status { get; set; }
        public string QrCode { get; set; }
    }

    /// <summary>
    /// Booking details response
    /// </summary>
    public class BookingDetailsResponse
    {
        public string BookingId { get; set; }
        public string Token { get; set; }
        public string CitizenName { get; set; }
        public string Mobile { get; set; }
        public string Date { get; set; }
        public string SlotTime { get; set; }
        public DateTime BookingTime { get; set; }
        public string Status { get; set; }
        public bool IsVerified { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string VerifiedBy { get; set; }
    }

    /// <summary>
    /// Department login response
    /// </summary>
    public class DepartmentLoginResponse
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Role { get; set; }
        public string AuthToken { get; set; }
        public DateTime TokenExpiry { get; set; }
        public List<string> Permissions { get; set; }

        public DepartmentLoginResponse()
        {
            Permissions = new List<string>();
        }
    }

    /// <summary>
    /// Dashboard statistics response
    /// </summary>
    public class DashboardStatsResponse
    {
        public int TotalBookings { get; set; }
        public int TodayBookings { get; set; }
        public int VerifiedEntries { get; set; }
        public int PendingEntries { get; set; }
        public List<SlotWiseBookingResponse> SlotWiseBookings { get; set; }
        public List<RecentBookingResponse> RecentBookings { get; set; }

        public DashboardStatsResponse()
        {
            SlotWiseBookings = new List<SlotWiseBookingResponse>();
            RecentBookings = new List<RecentBookingResponse>();
        }
    }

    /// <summary>
    /// Slot-wise booking response
    /// </summary>
    public class SlotWiseBookingResponse
    {
        public string SlotLabel { get; set; }
        public int Bookings { get; set; }
    }

    /// <summary>
    /// Recent booking response
    /// </summary>
    public class RecentBookingResponse
    {
        public string BookingId { get; set; }
        public string Token { get; set; }
        public string CitizenName { get; set; }
        public string Date { get; set; }
        public string SlotTime { get; set; }
        public string Status { get; set; }
    }

    /// <summary>
    /// Bookings list response
    /// </summary>
    public class BookingsListResponse
    {
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public List<BookingDetailsResponse> Bookings { get; set; }

        public BookingsListResponse()
        {
            Bookings = new List<BookingDetailsResponse>();
        }
    }

    /// <summary>
    /// Entry verification response
    /// </summary>
    public class VerifyEntryResponse
    {
        public string BookingId { get; set; }
        public string Token { get; set; }
        public bool IsVerified { get; set; }
        public DateTime VerifiedAt { get; set; }
        public string VerifiedBy { get; set; }
        public string VerifierName { get; set; }
    }

    /// <summary>
    /// Cancel booking response
    /// </summary>
    public class CancelBookingResponse
    {
        public string BookingId { get; set; }
        public string Status { get; set; }
        public DateTime CancelledAt { get; set; }
        public string CancelledBy { get; set; }
        public string Reason { get; set; }
    }

    /// <summary>
    /// Receipt response
    /// </summary>
    public class ReceiptResponse
    {
        public string ReceiptUrl { get; set; }
        public string ReceiptHtml { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    /// <summary>
    /// QR Code response
    /// </summary>
    public class QrCodeResponse
    {
        public string QrCodeBase64 { get; set; }
        public string QrCodeUrl { get; set; }
    }

    #endregion
}

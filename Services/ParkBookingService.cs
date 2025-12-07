using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SmkcApi.Models;
using SmkcApi.Repositories;
using SmkcApi.Infrastructure;

namespace SmkcApi.Services
{
    /// <summary>
    /// Park Booking Service Implementation
    /// Handles business logic for park booking operations
    /// </summary>
    public class ParkBookingService : IParkBookingService
    {
        private readonly IParkBookingRepository _repository;

        public ParkBookingService(IParkBookingRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        #region Citizen Operations

        public async Task<ApiResponse<CitizenRegistrationResponse>> RegisterCitizenAsync(CitizenRegistrationRequest request)
        {
            try
            {
                // Validation
                if (request == null)
                    return ApiResponse<CitizenRegistrationResponse>.CreateError("Request body is required", "MISSING_REQUEST");

                if (string.IsNullOrWhiteSpace(request.Name))
                    return ApiResponse<CitizenRegistrationResponse>.CreateError("Name is required", "MISSING_NAME");

                if (string.IsNullOrWhiteSpace(request.Mobile))
                    return ApiResponse<CitizenRegistrationResponse>.CreateError("Mobile number is required", "MISSING_MOBILE");

                if (!IsValidMobileNumber(request.Mobile))
                    return ApiResponse<CitizenRegistrationResponse>.CreateError("Invalid mobile number format. Must be 10 digits", "INVALID_MOBILE");

                // Register citizen
                var result = await _repository.RegisterCitizenAsync(request.Name.Trim(), request.Mobile.Trim());

                // Log SMS for OTP
                await LogSmsAsync(request.Mobile, $"Your OTP is {GenerateOtp()}. Valid for 5 minutes.", "OTP_TEMPLATE");

                return ApiResponse<CitizenRegistrationResponse>.CreateSuccess(result, "OTP sent successfully to mobile number");
            }
            catch (Exception ex)
            {
                return ApiResponse<CitizenRegistrationResponse>.CreateError($"Error during registration: {ex.Message}", "REGISTRATION_ERROR");
            }
        }

        public async Task<ApiResponse<OTPVerificationResponse>> VerifyOtpAsync(OTPVerificationRequest request)
        {
            try
            {
                if (request == null)
                    return ApiResponse<OTPVerificationResponse>.CreateError("Request body is required", "MISSING_REQUEST");

                if (string.IsNullOrWhiteSpace(request.CitizenId))
                    return ApiResponse<OTPVerificationResponse>.CreateError("Citizen ID is required", "MISSING_CITIZEN_ID");

                if (string.IsNullOrWhiteSpace(request.Mobile))
                    return ApiResponse<OTPVerificationResponse>.CreateError("Mobile number is required", "MISSING_MOBILE");

                if (!IsValidMobileNumber(request.Mobile))
                    return ApiResponse<OTPVerificationResponse>.CreateError("Invalid mobile number format", "INVALID_MOBILE");

                if (string.IsNullOrWhiteSpace(request.Otp) || request.Otp.Length != 4)
                    return ApiResponse<OTPVerificationResponse>.CreateError("Invalid OTP format", "INVALID_OTP");

                var result = await _repository.VerifyOtpAsync(request.CitizenId, request.Mobile, request.Otp);

                if (!result.IsVerified)
                    return ApiResponse<OTPVerificationResponse>.CreateError("Invalid or expired OTP", "OTP_VERIFICATION_FAILED");

                return ApiResponse<OTPVerificationResponse>.CreateSuccess(result, "OTP verified successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<OTPVerificationResponse>.CreateError($"Error during OTP verification: {ex.Message}", "OTP_VERIFICATION_ERROR");
            }
        }

        public async Task<ApiResponse<ResendOTPResponse>> ResendOtpAsync(ResendOTPRequest request)
        {
            try
            {
                if (request == null)
                    return ApiResponse<ResendOTPResponse>.CreateError("Request body is required", "MISSING_REQUEST");

                if (string.IsNullOrWhiteSpace(request.CitizenId))
                    return ApiResponse<ResendOTPResponse>.CreateError("Citizen ID is required", "MISSING_CITIZEN_ID");

                if (string.IsNullOrWhiteSpace(request.Mobile))
                    return ApiResponse<ResendOTPResponse>.CreateError("Mobile number is required", "MISSING_MOBILE");

                if (!IsValidMobileNumber(request.Mobile))
                    return ApiResponse<ResendOTPResponse>.CreateError("Invalid mobile number format", "INVALID_MOBILE");

                var result = await _repository.ResendOtpAsync(request.CitizenId, request.Mobile);

                // Log SMS for new OTP
                await LogSmsAsync(request.Mobile, $"Your new OTP is {GenerateOtp()}. Valid for 5 minutes.", "OTP_TEMPLATE");

                return ApiResponse<ResendOTPResponse>.CreateSuccess(result, "OTP resent successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<ResendOTPResponse>.CreateError($"Error resending OTP: {ex.Message}", "RESEND_OTP_ERROR");
            }
        }

        #endregion

        #region Slot Operations

        public async Task<ApiResponse<AvailableSlotsResponse>> GetAvailableSlotsAsync(string date)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(date))
                    return ApiResponse<AvailableSlotsResponse>.CreateError("Date is required", "MISSING_DATE");

                if (!DateTime.TryParse(date, out var parsedDate))
                    return ApiResponse<AvailableSlotsResponse>.CreateError("Invalid date format. Use YYYY-MM-DD", "INVALID_DATE_FORMAT");

                if (parsedDate < DateTime.Today)
                    return ApiResponse<AvailableSlotsResponse>.CreateError("Date cannot be in the past", "INVALID_DATE");

                var result = await _repository.GetAvailableSlotsAsync(date);

                return ApiResponse<AvailableSlotsResponse>.CreateSuccess(result, "Available slots retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<AvailableSlotsResponse>.CreateError($"Error fetching slots: {ex.Message}", "SLOTS_FETCH_ERROR");
            }
        }

        #endregion

        #region Booking Operations

        public async Task<ApiResponse<BookingCreateResponse>> CreateBookingAsync(BookingRequest request)
        {
            try
            {
                if (request == null)
                    return ApiResponse<BookingCreateResponse>.CreateError("Request body is required", "MISSING_REQUEST");

                if (string.IsNullOrWhiteSpace(request.CitizenId))
                    return ApiResponse<BookingCreateResponse>.CreateError("Citizen ID is required", "MISSING_CITIZEN_ID");

                if (string.IsNullOrWhiteSpace(request.Date))
                    return ApiResponse<BookingCreateResponse>.CreateError("Date is required", "MISSING_DATE");

                if (!DateTime.TryParse(request.Date, out var bookingDate))
                    return ApiResponse<BookingCreateResponse>.CreateError("Invalid date format", "INVALID_DATE_FORMAT");

                if (string.IsNullOrWhiteSpace(request.SlotId))
                    return ApiResponse<BookingCreateResponse>.CreateError("Slot ID is required", "MISSING_SLOT_ID");

                if (string.IsNullOrWhiteSpace(request.SlotCode))
                    return ApiResponse<BookingCreateResponse>.CreateError("Slot code is required", "MISSING_SLOT_CODE");

                var result = await _repository.CreateBookingAsync(request.CitizenId, request.Date, request.SlotId, request.SlotCode);

                // Generate QR code
                if (!string.IsNullOrEmpty(result.BookingId) && !string.IsNullOrEmpty(result.Token))
                {
                    result.QrCode = GenerateQrCodeBase64(result.Token);
                }

                return ApiResponse<BookingCreateResponse>.CreateSuccess(result, "Booking created successfully");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("fully booked"))
            {
                return ApiResponse<BookingCreateResponse>.CreateError("Slot is fully booked", "SLOT_FULL");
            }
            catch (Exception ex)
            {
                return ApiResponse<BookingCreateResponse>.CreateError($"Error creating booking: {ex.Message}", "BOOKING_CREATION_ERROR");
            }
        }

        public async Task<ApiResponse<BookingDetailsResponse>> GetBookingDetailsAsync(string bookingId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(bookingId))
                    return ApiResponse<BookingDetailsResponse>.CreateError("Booking ID is required", "MISSING_BOOKING_ID");

                var result = await _repository.GetBookingDetailsAsync(bookingId);

                if (result == null)
                    return ApiResponse<BookingDetailsResponse>.CreateError("Booking not found", "BOOKING_NOT_FOUND");

                return ApiResponse<BookingDetailsResponse>.CreateSuccess(result, "Booking details retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<BookingDetailsResponse>.CreateError($"Error fetching booking details: {ex.Message}", "BOOKING_FETCH_ERROR");
            }
        }

        public async Task<ApiResponse<BookingsListResponse>> GetAllBookingsAsync(BookingFilterRequest request)
        {
            try
            {
                if (request == null)
                    return ApiResponse<BookingsListResponse>.CreateError("Request is required", "MISSING_REQUEST");

                if (request.Page < 1)
                    return ApiResponse<BookingsListResponse>.CreateError("Page must be greater than 0", "INVALID_PAGE");

                if (request.PageSize < 1 || request.PageSize > 100)
                    return ApiResponse<BookingsListResponse>.CreateError("Page size must be between 1 and 100", "INVALID_PAGE_SIZE");

                var result = await _repository.GetAllBookingsAsync(
                    request.Page,
                    request.PageSize,
                    request.Status,
                    request.Date,
                    request.SearchToken,
                    request.SortBy,
                    request.SortOrder);

                return ApiResponse<BookingsListResponse>.CreateSuccess(result, "Bookings retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<BookingsListResponse>.CreateError($"Error fetching bookings: {ex.Message}", "BOOKINGS_FETCH_ERROR");
            }
        }

        public async Task<ApiResponse<BookingDetailsResponse>> SearchBookingByTokenAsync(string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                    return ApiResponse<BookingDetailsResponse>.CreateError("Token is required", "MISSING_TOKEN");

                var result = await _repository.SearchBookingByTokenAsync(token);

                if (result == null)
                    return ApiResponse<BookingDetailsResponse>.CreateError($"No booking found with token: {token}", "BOOKING_NOT_FOUND");

                return ApiResponse<BookingDetailsResponse>.CreateSuccess(result, "Booking found");
            }
            catch (Exception ex)
            {
                return ApiResponse<BookingDetailsResponse>.CreateError($"Error searching booking: {ex.Message}", "BOOKING_SEARCH_ERROR");
            }
        }

        public async Task<ApiResponse<VerifyEntryResponse>> VerifyEntryAsync(string bookingId, VerifyEntryRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(bookingId))
                    return ApiResponse<VerifyEntryResponse>.CreateError("Booking ID is required", "MISSING_BOOKING_ID");

                if (request == null)
                    return ApiResponse<VerifyEntryResponse>.CreateError("Request is required", "MISSING_REQUEST");

                if (string.IsNullOrWhiteSpace(request.Token))
                    return ApiResponse<VerifyEntryResponse>.CreateError("Token is required", "MISSING_TOKEN");

                if (string.IsNullOrWhiteSpace(request.VerifiedBy))
                    return ApiResponse<VerifyEntryResponse>.CreateError("VerifiedBy is required", "MISSING_VERIFIED_BY");

                var result = await _repository.VerifyEntryAsync(bookingId, request.Token, request.VerifiedBy, request.Notes ?? "");

                if (result == null)
                    return ApiResponse<VerifyEntryResponse>.CreateError("Booking verification failed", "VERIFICATION_FAILED");

                return ApiResponse<VerifyEntryResponse>.CreateSuccess(result, "Entry verified successfully");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already verified"))
            {
                return ApiResponse<VerifyEntryResponse>.CreateError("Booking is already verified", "ALREADY_VERIFIED");
            }
            catch (Exception ex)
            {
                return ApiResponse<VerifyEntryResponse>.CreateError($"Error verifying entry: {ex.Message}", "VERIFICATION_ERROR");
            }
        }

        public async Task<ApiResponse<CancelBookingResponse>> CancelBookingAsync(string bookingId, CancelBookingRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(bookingId))
                    return ApiResponse<CancelBookingResponse>.CreateError("Booking ID is required", "MISSING_BOOKING_ID");

                if (request == null)
                    return ApiResponse<CancelBookingResponse>.CreateError("Request is required", "MISSING_REQUEST");

                if (string.IsNullOrWhiteSpace(request.CancelledBy))
                    return ApiResponse<CancelBookingResponse>.CreateError("CancelledBy is required", "MISSING_CANCELLED_BY");

                var result = await _repository.CancelBookingAsync(bookingId, request.Reason ?? "", request.CancelledBy);

                return ApiResponse<CancelBookingResponse>.CreateSuccess(result, "Booking cancelled successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<CancelBookingResponse>.CreateError($"Error cancelling booking: {ex.Message}", "CANCELLATION_ERROR");
            }
        }

        #endregion

        #region Department Operations

        public async Task<ApiResponse<DepartmentLoginResponse>> DepartmentLoginAsync(DepartmentLoginRequest request)
        {
            try
            {
                if (request == null)
                    return ApiResponse<DepartmentLoginResponse>.CreateError("Request is required", "MISSING_REQUEST");

                if (string.IsNullOrWhiteSpace(request.UserId))
                    return ApiResponse<DepartmentLoginResponse>.CreateError("User ID is required", "MISSING_USER_ID");

                if (string.IsNullOrWhiteSpace(request.Password))
                    return ApiResponse<DepartmentLoginResponse>.CreateError("Password is required", "MISSING_PASSWORD");

                var result = await _repository.DepartmentLoginAsync(request.UserId, request.Password);

                return ApiResponse<DepartmentLoginResponse>.CreateSuccess(result, "Login successful");
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponse<DepartmentLoginResponse>.CreateError("Invalid credentials", "INVALID_CREDENTIALS");
            }
            catch (Exception ex)
            {
                return ApiResponse<DepartmentLoginResponse>.CreateError($"Error during login: {ex.Message}", "LOGIN_ERROR");
            }
        }

        public async Task<ApiResponse<DashboardStatsResponse>> GetDashboardStatsAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ApiResponse<DashboardStatsResponse>.CreateError("User ID is required", "MISSING_USER_ID");

                var result = await _repository.GetDashboardStatsAsync(userId);

                return ApiResponse<DashboardStatsResponse>.CreateSuccess(result, "Dashboard statistics retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<DashboardStatsResponse>.CreateError($"Error fetching dashboard stats: {ex.Message}", "STATS_FETCH_ERROR");
            }
        }

        #endregion

        #region Utility Operations

        public async Task<ApiResponse<ReceiptResponse>> GenerateReceiptAsync(string bookingId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(bookingId))
                    return ApiResponse<ReceiptResponse>.CreateError("Booking ID is required", "MISSING_BOOKING_ID");

                var result = await _repository.GenerateReceiptAsync(bookingId);

                if (result == null)
                    return ApiResponse<ReceiptResponse>.CreateError("Receipt generation failed", "RECEIPT_GENERATION_FAILED");

                return ApiResponse<ReceiptResponse>.CreateSuccess(result, "Receipt generated successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<ReceiptResponse>.CreateError($"Error generating receipt: {ex.Message}", "RECEIPT_ERROR");
            }
        }

        public async Task<ApiResponse<List<dynamic>>> GenerateBookingReportAsync(ReportRequest request)
        {
            try
            {
                if (request == null)
                    return ApiResponse<List<dynamic>>.CreateError("Request is required", "MISSING_REQUEST");

                if (string.IsNullOrWhiteSpace(request.StartDate))
                    return ApiResponse<List<dynamic>>.CreateError("Start date is required", "MISSING_START_DATE");

                if (string.IsNullOrWhiteSpace(request.EndDate))
                    return ApiResponse<List<dynamic>>.CreateError("End date is required", "MISSING_END_DATE");

                if (!DateTime.TryParse(request.StartDate, out var startDate) ||
                    !DateTime.TryParse(request.EndDate, out var endDate))
                    return ApiResponse<List<dynamic>>.CreateError("Invalid date format", "INVALID_DATE_FORMAT");

                if (startDate > endDate)
                    return ApiResponse<List<dynamic>>.CreateError("Start date cannot be greater than end date", "INVALID_DATE_RANGE");

                var result = await _repository.GenerateBookingReportAsync(request.StartDate, request.EndDate, request.ReportType);

                return ApiResponse<List<dynamic>>.CreateSuccess(result, "Report generated successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<List<dynamic>>.CreateError($"Error generating report: {ex.Message}", "REPORT_ERROR");
            }
        }

        public async Task<ApiResponse<object>> SendSmsAsync(SmsRequest request)
        {
            try
            {
                if (request == null)
                    return ApiResponse<object>.CreateError("Request is required", "MISSING_REQUEST");

                if (string.IsNullOrWhiteSpace(request.Mobile))
                    return ApiResponse<object>.CreateError("Mobile number is required", "MISSING_MOBILE");

                if (!IsValidMobileNumber(request.Mobile))
                    return ApiResponse<object>.CreateError("Invalid mobile number format", "INVALID_MOBILE");

                if (string.IsNullOrWhiteSpace(request.Message))
                    return ApiResponse<object>.CreateError("Message is required", "MISSING_MESSAGE");

                // Log SMS
                var smsId = await _repository.LogSmsAsync(request.Mobile, request.Message, request.TemplateId ?? "");

                // Note: Actual SMS sending would be implemented via the SmsSender
                // For now, we just log it in the database

                return ApiResponse<object>.CreateSuccess(
                    new { smsId, mobile = request.Mobile, sentAt = DateTime.Now },
                    "SMS sent successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.CreateError($"Error sending SMS: {ex.Message}", "SMS_ERROR");
            }
        }

        public async Task<ApiResponse<QrCodeResponse>> GenerateQrCodeAsync(QrCodeRequest request)
        {
            try
            {
                if (request == null)
                    return ApiResponse<QrCodeResponse>.CreateError("Request is required", "MISSING_REQUEST");

                if (string.IsNullOrWhiteSpace(request.Token))
                    return ApiResponse<QrCodeResponse>.CreateError("Token is required", "MISSING_TOKEN");

                if (string.IsNullOrWhiteSpace(request.BookingId))
                    return ApiResponse<QrCodeResponse>.CreateError("Booking ID is required", "MISSING_BOOKING_ID");

                var qrCodeBase64 = GenerateQrCodeBase64(request.Token);

                var response = new QrCodeResponse
                {
                    QrCodeBase64 = qrCodeBase64,
                    QrCodeUrl = $"https://api.park.com/qr/{request.BookingId}.png"
                };

                return ApiResponse<QrCodeResponse>.CreateSuccess(response, "QR code generated successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<QrCodeResponse>.CreateError($"Error generating QR code: {ex.Message}", "QR_CODE_ERROR");
            }
        }

        #endregion

        #region Helper Methods

        private bool IsValidMobileNumber(string mobile)
        {
            return !string.IsNullOrWhiteSpace(mobile) && Regex.IsMatch(mobile, @"^\d{10}$");
        }

        private string GenerateOtp()
        {
            var random = new Random();
            return random.Next(1000, 9999).ToString();
        }

        private string GenerateQrCodeBase64(string token)
        {
            // Placeholder for QR code generation
            // In production, use a QR code library like QRCoder
            return "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAA...";
        }

        private async Task LogSmsAsync(string mobile, string message, string templateId)
        {
            try
            {
                await _repository.LogSmsAsync(mobile, message, templateId);
            }
            catch
            {
                // Log SMS failure but don't fail the main operation
            }
        }

        #endregion
    }
}

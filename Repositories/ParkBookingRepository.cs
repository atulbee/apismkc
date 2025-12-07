using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using SmkcApi.Models;

namespace SmkcApi.Repositories
{
    /// <summary>
    /// Park Booking Repository Implementation
    /// Executes Oracle stored procedures for park booking operations
    /// </summary>
    public class ParkBookingRepository : IParkBookingRepository
    {
        private readonly OracleConnectionFactory _connectionFactory;

        public ParkBookingRepository(OracleConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        #region Citizen Management

        public async Task<CitizenRegistrationResponse> RegisterCitizenAsync(string name, string mobile)
        {
            try
            {
                using (var connection = _connectionFactory.Create())
                {
                    await connection.OpenAsync();

                    using (var command = new OracleCommand("PKG_PARK_BOOKING.SP_CITIZEN_REGISTER", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 30;

                        // Input parameters
                        command.Parameters.Add("P_NAME", OracleDbType.Varchar2).Value = name ?? string.Empty;
                        command.Parameters.Add("P_MOBILE", OracleDbType.Varchar2).Value = mobile ?? string.Empty;

                        // Output parameters
                        command.Parameters.Add("P_CITIZEN_ID", OracleDbType.Varchar2, 50).Direction = ParameterDirection.Output;
                        command.Parameters.Add("P_OTP", OracleDbType.Varchar2, 6).Direction = ParameterDirection.Output;
                        command.Parameters.Add("P_STATUS", OracleDbType.Int32).Direction = ParameterDirection.Output;
                        command.Parameters.Add("P_MESSAGE", OracleDbType.Varchar2, 500).Direction = ParameterDirection.Output;

                        await command.ExecuteNonQueryAsync();

                        var status = Convert.ToInt32(command.Parameters["P_STATUS"].Value);
                        var message = command.Parameters["P_MESSAGE"].Value?.ToString();

                        if (status != 1)
                            throw new InvalidOperationException(message ?? "Registration failed");

                        return new CitizenRegistrationResponse
                        {
                            CitizenId = command.Parameters["P_CITIZEN_ID"].Value?.ToString(),
                            Name = name,
                            Mobile = mobile,
                            OtpSentAt = DateTime.Now,
                            OtpExpiresAt = DateTime.Now.AddMinutes(5)
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error registering citizen: {ex.Message}", ex);
            }
        }

        public async Task<OTPVerificationResponse> VerifyOtpAsync(string citizenId, string mobile, string otp)
        {
            try
            {
                using (var connection = _connectionFactory.Create())
                {
                    await connection.OpenAsync();

                    using (var command = new OracleCommand("PKG_PARK_BOOKING.SP_VERIFY_OTP", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 30;

                        command.Parameters.Add("P_CITIZEN_ID", OracleDbType.Varchar2).Value = citizenId ?? string.Empty;
                        command.Parameters.Add("P_MOBILE", OracleDbType.Varchar2).Value = mobile ?? string.Empty;
                        command.Parameters.Add("P_OTP", OracleDbType.Varchar2).Value = otp ?? string.Empty;

                        command.Parameters.Add("P_IS_VALID", OracleDbType.Int32).Direction = ParameterDirection.Output;
                        command.Parameters.Add("P_AUTH_TOKEN", OracleDbType.Varchar2, 500).Direction = ParameterDirection.Output;
                        command.Parameters.Add("P_STATUS", OracleDbType.Int32).Direction = ParameterDirection.Output;
                        command.Parameters.Add("P_MESSAGE", OracleDbType.Varchar2, 500).Direction = ParameterDirection.Output;

                        await command.ExecuteNonQueryAsync();

                        var status = Convert.ToInt32(command.Parameters["P_STATUS"].Value);
                        var message = command.Parameters["P_MESSAGE"].Value?.ToString();

                        if (status != 1)
                            throw new InvalidOperationException(message ?? "OTP verification failed");

                        var isValid = Convert.ToInt32(command.Parameters["P_IS_VALID"].Value) == 1;

                        return new OTPVerificationResponse
                        {
                            CitizenId = citizenId,
                            IsVerified = isValid,
                            AuthToken = command.Parameters["P_AUTH_TOKEN"].Value?.ToString(),
                            TokenExpiry = DateTime.Now.AddHours(12)
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error verifying OTP: {ex.Message}", ex);
            }
        }

        public async Task<ResendOTPResponse> ResendOtpAsync(string citizenId, string mobile)
        {
            try
            {
                using (var connection = _connectionFactory.Create())
                {
                    await connection.OpenAsync();

                    using (var command = new OracleCommand("PKG_PARK_BOOKING.SP_RESEND_OTP", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 30;

                        command.Parameters.Add("P_CITIZEN_ID", OracleDbType.Varchar2).Value = citizenId ?? string.Empty;
                        command.Parameters.Add("P_MOBILE", OracleDbType.Varchar2).Value = mobile ?? string.Empty;

                        command.Parameters.Add("P_NEW_OTP", OracleDbType.Varchar2, 6).Direction = ParameterDirection.Output;
                        command.Parameters.Add("P_ATTEMPTS_LEFT", OracleDbType.Int32).Direction = ParameterDirection.Output;
                        command.Parameters.Add("P_STATUS", OracleDbType.Int32).Direction = ParameterDirection.Output;
                        command.Parameters.Add("P_MESSAGE", OracleDbType.Varchar2, 500).Direction = ParameterDirection.Output;

                        await command.ExecuteNonQueryAsync();

                        var status = Convert.ToInt32(command.Parameters["P_STATUS"].Value);
                        var message = command.Parameters["P_MESSAGE"].Value?.ToString();

                        if (status != 1)
                            throw new InvalidOperationException(message ?? "OTP resend failed");

                        return new ResendOTPResponse
                        {
                            OtpSentAt = DateTime.Now,
                            OtpExpiresAt = DateTime.Now.AddMinutes(5),
                            AttemptsRemaining = Convert.ToInt32(command.Parameters["P_ATTEMPTS_LEFT"].Value ?? 0)
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error resending OTP: {ex.Message}", ex);
            }
        }

        #endregion

        #region Slot Management

        public async Task<AvailableSlotsResponse> GetAvailableSlotsAsync(string date)
        {
            try
            {
                var slotsResponse = new AvailableSlotsResponse { Date = date };

                using (var connection = _connectionFactory.Create())
                {
                    await connection.OpenAsync();

                    using (var command = new OracleCommand("PKG_PARK_BOOKING.SP_GET_AVAILABLE_SLOTS", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 30;

                        if (!DateTime.TryParse(date, out var parsedDate))
                            throw new ArgumentException("Invalid date format");

                        command.Parameters.Add("P_DATE", OracleDbType.Date).Value = parsedDate;
                        command.Parameters.Add("P_SLOTS", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
                        command.Parameters.Add("P_STATUS", OracleDbType.Int32).Direction = ParameterDirection.Output;
                        command.Parameters.Add("P_MESSAGE", OracleDbType.Varchar2, 500).Direction = ParameterDirection.Output;

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                slotsResponse.Slots.Add(new TimeSlotResponse
                                {
                                    SlotId = reader["SLOT_ID"]?.ToString(),
                                    SlotCode = reader["SLOT_CODE"]?.ToString(),
                                    SlotTime = reader["SLOT_TIME"]?.ToString(),
                                    SlotLabel = reader["SLOT_LABEL"]?.ToString(),
                                    TotalCapacity = Convert.ToInt32(reader["TOTAL_CAPACITY"] ?? 0),
                                    BookedCount = Convert.ToInt32(reader["BOOKED_COUNT"] ?? 0),
                                    AvailableSeats = Convert.ToInt32(reader["AVAILABLE_SEATS"] ?? 0),
                                    IsAvailable = Convert.ToInt32(reader["IS_AVAILABLE"] ?? 0) == 1
                                });
                            }
                        }

                        return slotsResponse;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error fetching available slots: {ex.Message}", ex);
            }
        }

        #endregion

        #region Booking Management

        public async Task<BookingCreateResponse> CreateBookingAsync(string citizenId, string date, string slotId, string slotCode)
        {
            try
            {
                using (var connection = _connectionFactory.Create())
                {
                    await connection.OpenAsync();

                    using (var command = new OracleCommand("PKG_PARK_BOOKING.SP_CREATE_BOOKING", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 30;

                        if (!DateTime.TryParse(date, out var parsedDate))
                            throw new ArgumentException("Invalid date format");

                        command.Parameters.Add("P_CITIZEN_ID", OracleDbType.Varchar2).Value = citizenId ?? string.Empty;
                        command.Parameters.Add("P_DATE", OracleDbType.Date).Value = parsedDate;
                        command.Parameters.Add("P_SLOT_ID", OracleDbType.Varchar2).Value = slotId ?? string.Empty;
                        command.Parameters.Add("P_SLOT_CODE", OracleDbType.Varchar2).Value = slotCode ?? string.Empty;

                        command.Parameters.Add("P_BOOKING_ID", OracleDbType.Varchar2, 50).Direction = ParameterDirection.Output;
                        command.Parameters.Add("P_TOKEN", OracleDbType.Varchar2, 50).Direction = ParameterDirection.Output;
                        command.Parameters.Add("P_STATUS", OracleDbType.Int32).Direction = ParameterDirection.Output;
                        command.Parameters.Add("P_MESSAGE", OracleDbType.Varchar2, 500).Direction = ParameterDirection.Output;

                        await command.ExecuteNonQueryAsync();

                        var status = Convert.ToInt32(command.Parameters["P_STATUS"].Value);
                        var message = command.Parameters["P_MESSAGE"].Value?.ToString();

                        if (status != 1)
                            throw new InvalidOperationException(message ?? "Booking creation failed");

                        return new BookingCreateResponse
                        {
                            BookingId = command.Parameters["P_BOOKING_ID"].Value?.ToString(),
                            Token = command.Parameters["P_TOKEN"].Value?.ToString(),
                            Status = "CONFIRMED",
                            BookingTime = DateTime.Now
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error creating booking: {ex.Message}", ex);
            }
        }

        public async Task<BookingDetailsResponse> GetBookingDetailsAsync(string bookingId)
        {
            try
            {
                using (var connection = _connectionFactory.Create())
                {
                    await connection.OpenAsync();

                    using (var command = new OracleCommand("PKG_PARK_BOOKING.SP_GET_BOOKING_DETAILS", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 30;

                        command.Parameters.Add("P_BOOKING_ID", OracleDbType.Varchar2).Value = bookingId ?? string.Empty;
                        command.Parameters.Add("P_BOOKING_DATA", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
                        command.Parameters.Add("P_STATUS", OracleDbType.Int32).Direction = ParameterDirection.Output;
                        command.Parameters.Add("P_MESSAGE", OracleDbType.Varchar2, 500).Direction = ParameterDirection.Output;

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new BookingDetailsResponse
                                {
                                    BookingId = reader["BOOKING_ID"]?.ToString(),
                                    Token = reader["TOKEN"]?.ToString(),
                                    CitizenName = reader["CITIZEN_NAME"]?.ToString(),
                                    Mobile = reader["MOBILE"]?.ToString(),
                                    Date = reader["DATE"]?.ToString(),
                                    SlotTime = reader["SLOT_TIME"]?.ToString(),
                                    BookingTime = Convert.ToDateTime(reader["BOOKING_TIME"] ?? DateTime.Now),
                                    Status = reader["STATUS"]?.ToString(),
                                    IsVerified = Convert.ToInt32(reader["IS_VERIFIED"] ?? 0) == 1,
                                    VerifiedAt = reader["VERIFIED_AT"] != DBNull.Value ? Convert.ToDateTime(reader["VERIFIED_AT"]) : (DateTime?)null,
                                    VerifiedBy = reader["VERIFIED_BY"]?.ToString()
                                };
                            }
                        }

                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error fetching booking details: {ex.Message}", ex);
            }
        }

        public async Task<BookingsListResponse> GetAllBookingsAsync(int page, int pageSize, string status, string date,
            string searchToken, string sortBy, string sortOrder)
        {
            try
            {
                var response = new BookingsListResponse { CurrentPage = page, PageSize = pageSize };

                using (var connection = _connectionFactory.Create())
                {
                    await connection.OpenAsync();

                    using (var command = new OracleCommand("PKG_PARK_BOOKING.SP_GET_ALL_BOOKINGS", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 30;

                        command.Parameters.Add("P_PAGE", OracleDbType.Int32).Value = page;
                        command.Parameters.Add("P_PAGE_SIZE", OracleDbType.Int32).Value = pageSize;
                        command.Parameters.Add("P_STATUS_FILTER", OracleDbType.Varchar2).Value = status ?? string.Empty;
                        command.Parameters.Add("P_DATE", OracleDbType.Date).Value = string.IsNullOrEmpty(date) ? DBNull.Value : (object)DateTime.Parse(date);
                        command.Parameters.Add("P_SEARCH_TOKEN", OracleDbType.Varchar2).Value = searchToken ?? string.Empty;
                        command.Parameters.Add("P_SORT_BY", OracleDbType.Varchar2).Value = sortBy ?? "bookingTime";
                        command.Parameters.Add("P_SORT_ORDER", OracleDbType.Varchar2).Value = sortOrder ?? "desc";

                        command.Parameters.Add("P_BOOKINGS", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
                        command.Parameters.Add("P_TOTAL_RECORDS", OracleDbType.Int32).Direction = ParameterDirection.Output;
                        command.Parameters.Add("P_STATUS", OracleDbType.Int32).Direction = ParameterDirection.Output;
                        command.Parameters.Add("P_MESSAGE", OracleDbType.Varchar2, 500).Direction = ParameterDirection.Output;

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                response.Bookings.Add(new BookingDetailsResponse
                                {
                                    BookingId = reader["BOOKING_ID"]?.ToString(),
                                    Token = reader["TOKEN"]?.ToString(),
                                    CitizenName = reader["CITIZEN_NAME"]?.ToString(),
                                    Mobile = reader["MOBILE"]?.ToString(),
                                    Date = reader["DATE"]?.ToString(),
                                    SlotTime = reader["SLOT_TIME"]?.ToString(),
                                    BookingTime = Convert.ToDateTime(reader["BOOKING_TIME"] ?? DateTime.Now),
                                    Status = reader["STATUS"]?.ToString(),
                                    IsVerified = Convert.ToInt32(reader["IS_VERIFIED"] ?? 0) == 1
                                });
                            }
                        }

                        response.TotalRecords = Convert.ToInt32(command.Parameters["P_TOTAL_RECORDS"].Value ?? 0);
                        response.TotalPages = (response.TotalRecords + pageSize - 1) / pageSize;

                        return response;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error fetching bookings: {ex.Message}", ex);
            }
        }

        public async Task<BookingDetailsResponse> SearchBookingByTokenAsync(string token)
        {
            try
            {
                using (var connection = _connectionFactory.Create())
                {
                    await connection.OpenAsync();

                    using (var command = new OracleCommand("PKG_PARK_BOOKING.SP_SEARCH_BOOKING_BY_TOKEN", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 30;

                        command.Parameters.Add("P_TOKEN", OracleDbType.Varchar2).Value = token ?? string.Empty;
                        command.Parameters.Add("P_BOOKING_DATA", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
                        command.Parameters.Add("P_STATUS", OracleDbType.Int32).Direction = ParameterDirection.Output;
                        command.Parameters.Add("P_MESSAGE", OracleDbType.Varchar2, 500).Direction = ParameterDirection.Output;

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new BookingDetailsResponse
                                {
                                    BookingId = reader["BOOKING_ID"]?.ToString(),
                                    Token = reader["TOKEN"]?.ToString(),
                                    CitizenName = reader["CITIZEN_NAME"]?.ToString(),
                                    Mobile = reader["MOBILE"]?.ToString(),
                                    Date = reader["DATE"]?.ToString(),
                                    SlotTime = reader["SLOT_TIME"]?.ToString(),
                                    BookingTime = Convert.ToDateTime(reader["BOOKING_TIME"] ?? DateTime.Now),
                                    Status = reader["STATUS"]?.ToString(),
                                    IsVerified = Convert.ToInt32(reader["IS_VERIFIED"] ?? 0) == 1
                                };
                            }
                        }

                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error searching booking: {ex.Message}", ex);
            }
        }

        public async Task<VerifyEntryResponse> VerifyEntryAsync(string bookingId, string token, string verifiedBy, string notes)
        {
            try
            {
                using (var connection = _connectionFactory.Create())
                {
                    await connection.OpenAsync();

                    using (var command = new OracleCommand("PKG_PARK_BOOKING.SP_VERIFY_ENTRY", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 30;

                        command.Parameters.Add("P_BOOKING_ID", OracleDbType.Varchar2).Value = bookingId ?? string.Empty;
                        command.Parameters.Add("P_TOKEN", OracleDbType.Varchar2).Value = token ?? string.Empty;
                        command.Parameters.Add("P_VERIFIED_BY", OracleDbType.Varchar2).Value = verifiedBy ?? string.Empty;
                        command.Parameters.Add("P_NOTES", OracleDbType.Varchar2).Value = notes ?? string.Empty;

                        command.Parameters.Add("P_VERIFICATION_DATA", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
                        command.Parameters.Add("P_STATUS", OracleDbType.Int32).Direction = ParameterDirection.Output;
                        command.Parameters.Add("P_MESSAGE", OracleDbType.Varchar2, 500).Direction = ParameterDirection.Output;

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new VerifyEntryResponse
                                {
                                    BookingId = reader["BOOKING_ID"]?.ToString(),
                                    Token = reader["TOKEN"]?.ToString(),
                                    IsVerified = Convert.ToInt32(reader["IS_VERIFIED"] ?? 0) == 1,
                                    VerifiedAt = Convert.ToDateTime(reader["VERIFIED_AT"] ?? DateTime.Now),
                                    VerifiedBy = reader["VERIFIED_BY"]?.ToString(),
                                    VerifierName = reader["VERIFIER_NAME"]?.ToString()
                                };
                            }
                        }

                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error verifying entry: {ex.Message}", ex);
            }
        }

        public async Task<CancelBookingResponse> CancelBookingAsync(string bookingId, string reason, string cancelledBy)
        {
            try
            {
                using (var connection = _connectionFactory.Create())
                {
                    await connection.OpenAsync();

                    using (var command = new OracleCommand("PKG_PARK_BOOKING.SP_CANCEL_BOOKING", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 30;

                        command.Parameters.Add("P_BOOKING_ID", OracleDbType.Varchar2).Value = bookingId ?? string.Empty;
                        command.Parameters.Add("P_REASON", OracleDbType.Varchar2).Value = reason ?? string.Empty;
                        command.Parameters.Add("P_CANCELLED_BY", OracleDbType.Varchar2).Value = cancelledBy ?? string.Empty;

                        command.Parameters.Add("P_STATUS", OracleDbType.Int32).Direction = ParameterDirection.Output;
                        command.Parameters.Add("P_MESSAGE", OracleDbType.Varchar2, 500).Direction = ParameterDirection.Output;

                        await command.ExecuteNonQueryAsync();

                        var status = Convert.ToInt32(command.Parameters["P_STATUS"].Value);
                        var message = command.Parameters["P_MESSAGE"].Value?.ToString();

                        if (status != 1)
                            throw new InvalidOperationException(message ?? "Booking cancellation failed");

                        return new CancelBookingResponse
                        {
                            BookingId = bookingId,
                            Status = "CANCELLED",
                            CancelledAt = DateTime.Now,
                            CancelledBy = cancelledBy,
                            Reason = reason
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error cancelling booking: {ex.Message}", ex);
            }
        }

        #endregion

        #region Department Management

        public async Task<DepartmentLoginResponse> DepartmentLoginAsync(string userId, string password)
        {
            try
            {
                using (var connection = _connectionFactory.Create())
                {
                    await connection.OpenAsync();

                    using (var command = new OracleCommand("PKG_PARK_BOOKING.SP_DEPARTMENT_LOGIN", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 30;

                        command.Parameters.Add("P_USER_ID", OracleDbType.Varchar2).Value = userId ?? string.Empty;
                        command.Parameters.Add("P_PASSWORD", OracleDbType.Varchar2).Value = password ?? string.Empty;

                        command.Parameters.Add("P_USER_DATA", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
                        command.Parameters.Add("P_AUTH_TOKEN", OracleDbType.Varchar2, 500).Direction = ParameterDirection.Output;
                        command.Parameters.Add("P_STATUS", OracleDbType.Int32).Direction = ParameterDirection.Output;
                        command.Parameters.Add("P_MESSAGE", OracleDbType.Varchar2, 500).Direction = ParameterDirection.Output;

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var response = new DepartmentLoginResponse
                                {
                                    UserId = reader["USER_ID"]?.ToString(),
                                    UserName = reader["USER_NAME"]?.ToString(),
                                    Role = reader["ROLE"]?.ToString(),
                                    AuthToken = command.Parameters["P_AUTH_TOKEN"].Value?.ToString(),
                                    TokenExpiry = DateTime.Now.AddHours(12)
                                };

                                // Parse permissions if available
                                var permissionsStr = reader["PERMISSIONS"]?.ToString();
                                if (!string.IsNullOrEmpty(permissionsStr))
                                {
                                    response.Permissions.AddRange(permissionsStr.Split(','));
                                }

                                return response;
                            }
                        }

                        throw new InvalidOperationException("Invalid credentials");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error during department login: {ex.Message}", ex);
            }
        }

        public async Task<DashboardStatsResponse> GetDashboardStatsAsync(string userId)
        {
            try
            {
                var response = new DashboardStatsResponse();

                using (var connection = _connectionFactory.Create())
                {
                    await connection.OpenAsync();

                    using (var command = new OracleCommand("PKG_PARK_BOOKING.SP_GET_DASHBOARD_STATS", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 30;

                        command.Parameters.Add("P_USER_ID", OracleDbType.Varchar2).Value = userId ?? string.Empty;
                        command.Parameters.Add("P_STATS_DATA", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
                        command.Parameters.Add("P_SLOT_DATA", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
                        command.Parameters.Add("P_RECENT_BOOKINGS", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
                        command.Parameters.Add("P_STATUS", OracleDbType.Int32).Direction = ParameterDirection.Output;
                        command.Parameters.Add("P_MESSAGE", OracleDbType.Varchar2, 500).Direction = ParameterDirection.Output;

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            // Read statistics
                            if (await reader.ReadAsync())
                            {
                                response.TotalBookings = Convert.ToInt32(reader["TOTAL_BOOKINGS"] ?? 0);
                                response.TodayBookings = Convert.ToInt32(reader["TODAY_BOOKINGS"] ?? 0);
                                response.VerifiedEntries = Convert.ToInt32(reader["VERIFIED_ENTRIES"] ?? 0);
                                response.PendingEntries = Convert.ToInt32(reader["PENDING_ENTRIES"] ?? 0);
                            }

                            // Read slot-wise bookings
                            if (reader.NextResult())
                            {
                                while (await reader.ReadAsync())
                                {
                                    response.SlotWiseBookings.Add(new SlotWiseBookingResponse
                                    {
                                        SlotLabel = reader["SLOT_LABEL"]?.ToString(),
                                        Bookings = Convert.ToInt32(reader["BOOKING_COUNT"] ?? 0)
                                    });
                                }
                            }

                            // Read recent bookings
                            if (reader.NextResult())
                            {
                                while (await reader.ReadAsync())
                                {
                                    response.RecentBookings.Add(new RecentBookingResponse
                                    {
                                        BookingId = reader["BOOKING_ID"]?.ToString(),
                                        Token = reader["TOKEN"]?.ToString(),
                                        CitizenName = reader["CITIZEN_NAME"]?.ToString(),
                                        Date = reader["DATE"]?.ToString(),
                                        SlotTime = reader["SLOT_TIME"]?.ToString(),
                                        Status = reader["STATUS"]?.ToString()
                                    });
                                }
                            }
                        }

                        return response;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error fetching dashboard stats: {ex.Message}", ex);
            }
        }

        #endregion

        #region Utility Functions

        public async Task<ReceiptResponse> GenerateReceiptAsync(string bookingId)
        {
            try
            {
                using (var connection = _connectionFactory.Create())
                {
                    await connection.OpenAsync();

                    using (var command = new OracleCommand("PKG_PARK_BOOKING.SP_GENERATE_RECEIPT", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 30;

                        command.Parameters.Add("P_BOOKING_ID", OracleDbType.Varchar2).Value = bookingId ?? string.Empty;
                        command.Parameters.Add("P_RECEIPT_DATA", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
                        command.Parameters.Add("P_STATUS", OracleDbType.Int32).Direction = ParameterDirection.Output;
                        command.Parameters.Add("P_MESSAGE", OracleDbType.Varchar2, 500).Direction = ParameterDirection.Output;

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new ReceiptResponse
                                {
                                    ReceiptUrl = reader["RECEIPT_URL"]?.ToString(),
                                    ReceiptHtml = reader["RECEIPT_HTML"]?.ToString(),
                                    ExpiresAt = DateTime.Now.AddHours(24)
                                };
                            }
                        }

                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error generating receipt: {ex.Message}", ex);
            }
        }

        public async Task<List<dynamic>> GenerateBookingReportAsync(string startDate, string endDate, string reportType)
        {
            try
            {
                var reportData = new List<dynamic>();

                using (var connection = _connectionFactory.Create())
                {
                    await connection.OpenAsync();

                    using (var command = new OracleCommand("PKG_PARK_BOOKING.SP_GENERATE_BOOKING_REPORT", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 30;

                        command.Parameters.Add("P_START_DATE", OracleDbType.Date).Value = DateTime.Parse(startDate);
                        command.Parameters.Add("P_END_DATE", OracleDbType.Date).Value = DateTime.Parse(endDate);
                        command.Parameters.Add("P_REPORT_TYPE", OracleDbType.Varchar2).Value = reportType ?? "CUSTOM";
                        command.Parameters.Add("P_REPORT_DATA", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
                        command.Parameters.Add("P_STATUS", OracleDbType.Int32).Direction = ParameterDirection.Output;
                        command.Parameters.Add("P_MESSAGE", OracleDbType.Varchar2, 500).Direction = ParameterDirection.Output;

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                // Return raw data for report processing
                                reportData.Add(reader);
                            }
                        }

                        return reportData;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error generating report: {ex.Message}", ex);
            }
        }

        public async Task<string> LogSmsAsync(string mobile, string message, string templateId)
        {
            try
            {
                using (var connection = _connectionFactory.Create())
                {
                    await connection.OpenAsync();

                    using (var command = new OracleCommand("PKG_PARK_BOOKING.SP_LOG_SMS", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 30;

                        command.Parameters.Add("P_MOBILE", OracleDbType.Varchar2).Value = mobile ?? string.Empty;
                        command.Parameters.Add("P_MESSAGE", OracleDbType.Clob).Value = message ?? string.Empty;
                        command.Parameters.Add("P_TEMPLATE_ID", OracleDbType.Varchar2).Value = templateId ?? string.Empty;

                        command.Parameters.Add("P_SMS_ID", OracleDbType.Varchar2, 50).Direction = ParameterDirection.Output;
                        command.Parameters.Add("P_STATUS", OracleDbType.Int32).Direction = ParameterDirection.Output;
                        command.Parameters.Add("P_MESSAGE", OracleDbType.Varchar2, 500).Direction = ParameterDirection.Output;

                        await command.ExecuteNonQueryAsync();

                        return command.Parameters["P_SMS_ID"].Value?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error logging SMS: {ex.Message}", ex);
            }
        }

        #endregion
    }
}

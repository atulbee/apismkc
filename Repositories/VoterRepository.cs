using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using SmkcApi.Models;

namespace SmkcApi.Repositories
{
    /// <summary>
    /// Voter Repository Implementation
    /// Executes Oracle stored procedures and SQL for duplicate voter management
    /// </summary>
    public class VoterRepository : IVoterRepository
    {
        private readonly IOracleConnectionFactory _connectionFactory;

        public VoterRepository(IOracleConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public async Task<FindDuplicatesResponse> FindDuplicateVotersAsync(string firstName, string middleName, string lastName)
        {
            try
            {
                var response = new FindDuplicatesResponse();

                using (var connection = _connectionFactory.Create())
                {
                    await connection.OpenAsync();

                    using (var command = new OracleCommand("PROC_FIND_DUPLICATE_VOTERS", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 60;

                        // Input parameters
                        command.Parameters.Add("P_FIRST_NAME", OracleDbType.Varchar2).Value = 
                            string.IsNullOrWhiteSpace(firstName) ? (object)DBNull.Value : firstName;
                        command.Parameters.Add("P_MIDDLE_NAME", OracleDbType.Varchar2).Value = 
                            string.IsNullOrWhiteSpace(middleName) ? (object)DBNull.Value : middleName;
                        command.Parameters.Add("P_LAST_NAME", OracleDbType.Varchar2).Value = 
                            string.IsNullOrWhiteSpace(lastName) ? (object)DBNull.Value : lastName;

                        // Output parameters
                        command.Parameters.Add("P_RESULT_CURSOR", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
                        var countParam = command.Parameters.Add("P_DUPLICATE_COUNT", OracleDbType.Decimal);
                        countParam.Direction = ParameterDirection.Output;

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                response.Records.Add(MapVoterRecord((OracleDataReader)reader));
                            }
                        }

                        // Safely convert OracleDecimal to int
                        var countValue = countParam.Value;
                        if (countValue != null && countValue != DBNull.Value)
                        {
                            if (countValue is OracleDecimal oracleDecimal)
                            {
                                response.DuplicateCount = oracleDecimal.IsNull ? 0 : (int)oracleDecimal.Value;
                            }
                            else
                            {
                                response.DuplicateCount = Convert.ToInt32(countValue);
                            }
                        }
                        else
                        {
                            response.DuplicateCount = response.Records.Count;
                        }

                        return response;
                    }
                }
            }
            catch (OracleException oex)
            {
                // Log Oracle-specific error details
                var errorDetails = $"Oracle Error: {oex.Message}, Number: {oex.Number}, Source: {oex.Source}";
                System.Diagnostics.Trace.TraceError($"FindDuplicateVotersAsync - {errorDetails}");
                throw new InvalidOperationException($"Database error finding duplicate voters: {oex.Message} (Error #{oex.Number})", oex);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"FindDuplicateVotersAsync - Error: {ex.Message}");
                throw new InvalidOperationException($"Error finding duplicate voters: {ex.Message}", ex);
            }
        }

        public async Task<MarkDuplicatesResponse> MarkDuplicatesAsync(List<int> srNoArray, bool isDuplicate, string remarks)
        {
            try
            {
                using (var connection = _connectionFactory.Create())
                {
                    await connection.OpenAsync();

                    using (var command = new OracleCommand("PROC_MARK_DUPLICATES", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 60;

                        // Pass SR_NO list as comma-separated string (procedure changed to accept string)
                        var srNoList = srNoArray == null || srNoArray.Count == 0
                            ? string.Empty
                            : string.Join(",", srNoArray);
                        command.Parameters.Add("P_SR_NO_LIST", OracleDbType.Varchar2).Value = srNoList;

                        // Other input parameters
                        command.Parameters.Add("P_IS_DUPLICATE", OracleDbType.Varchar2).Value = isDuplicate ? "TRUE" : "FALSE";
                        command.Parameters.Add("P_REMARKS", OracleDbType.Varchar2).Value = 
                            string.IsNullOrWhiteSpace(remarks) ? (object)DBNull.Value : remarks;

                        // Output parameters
                        var dupIdParam = command.Parameters.Add("P_DUPLICATION_ID", OracleDbType.Decimal);
                        dupIdParam.Direction = ParameterDirection.Output;
                        
                        var statusParam = command.Parameters.Add("P_STATUS", OracleDbType.Varchar2, 500);
                        statusParam.Direction = ParameterDirection.Output;

                        await command.ExecuteNonQueryAsync();

                        // Handle OracleDecimal for duplication ID
                        int? duplicationId = null;
                        var duplicationIdValue = dupIdParam.Value;
                        if (duplicationIdValue != null && duplicationIdValue != DBNull.Value)
                        {
                            if (duplicationIdValue is OracleDecimal oracleDecimal && !oracleDecimal.IsNull)
                            {
                                duplicationId = (int)oracleDecimal.Value;
                            }
                        }

                        return new MarkDuplicatesResponse
                        {
                            Status = statusParam.Value?.ToString() ?? "Unknown",
                            DuplicationId = duplicationId
                        };
                    }
                }
            }
            catch (OracleException oex)
            {
                var errorDetails = $"Oracle Error: {oex.Message}, Number: {oex.Number}";
                System.Diagnostics.Trace.TraceError($"MarkDuplicatesAsync - {errorDetails}");
                throw new InvalidOperationException($"Database error marking duplicates: {oex.Message} (Error #{oex.Number})", oex);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"MarkDuplicatesAsync - Error: {ex.Message}");
                throw new InvalidOperationException($"Error marking duplicates: {ex.Message}", ex);
            }
        }

        public async Task<VerificationStatusResponse> GetVerificationStatusAsync()
        {
            try
            {
                using (var connection = _connectionFactory.Create())
                {
                    await connection.OpenAsync();

                    using (var command = new OracleCommand("PROC_GET_VERIFICATION_STATUS", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 30;

                        // Output parameters - use Decimal for Oracle NUMBER type
                        var totalParam = command.Parameters.Add("P_TOTAL_RECORDS", OracleDbType.Decimal);
                        totalParam.Direction = ParameterDirection.Output;
                        
                        var verifiedParam = command.Parameters.Add("P_VERIFIED_RECORDS", OracleDbType.Decimal);
                        verifiedParam.Direction = ParameterDirection.Output;
                        
                        var unverifiedParam = command.Parameters.Add("P_UNVERIFIED_RECORDS", OracleDbType.Decimal);
                        unverifiedParam.Direction = ParameterDirection.Output;
                        
                        var duplicateParam = command.Parameters.Add("P_DUPLICATE_RECORDS", OracleDbType.Decimal);
                        duplicateParam.Direction = ParameterDirection.Output;
                        
                        var notDuplicateParam = command.Parameters.Add("P_NOT_DUPLICATE_RECORDS", OracleDbType.Decimal);
                        notDuplicateParam.Direction = ParameterDirection.Output;
                        
                        var percentageParam = command.Parameters.Add("P_VERIFICATION_PERCENTAGE", OracleDbType.Decimal);
                        percentageParam.Direction = ParameterDirection.Output;

                        await command.ExecuteNonQueryAsync();

                        return new VerificationStatusResponse
                        {
                            TotalRecords = ConvertOracleDecimalToInt(totalParam.Value),
                            VerifiedRecords = ConvertOracleDecimalToInt(verifiedParam.Value),
                            UnverifiedRecords = ConvertOracleDecimalToInt(unverifiedParam.Value),
                            DuplicateRecords = ConvertOracleDecimalToInt(duplicateParam.Value),
                            NotDuplicateRecords = ConvertOracleDecimalToInt(notDuplicateParam.Value),
                            VerificationPercentage = ConvertOracleDecimalToDecimal(percentageParam.Value)
                        };
                    }
                }
            }
            catch (OracleException oex)
            {
                var errorDetails = $"Oracle Error: {oex.Message}, Number: {oex.Number}";
                System.Diagnostics.Trace.TraceError($"GetVerificationStatusAsync - {errorDetails}");
                throw new InvalidOperationException($"Database error getting verification status: {oex.Message} (Error #{oex.Number})", oex);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"GetVerificationStatusAsync - Error: {ex.Message}");
                throw new InvalidOperationException($"Error getting verification status: {ex.Message}", ex);
            }
        }

        public async Task<DuplicateGroupsResponse> GetDuplicateGroupsAsync(int? duplicationId = null)
        {
            try
            {
                var response = new DuplicateGroupsResponse();
                var groupDict = new Dictionary<int, DuplicateGroup>();

                using (var connection = _connectionFactory.Create())
                {
                    await connection.OpenAsync();

                    var sql = @"SELECT dr.DUPLICATION_ID, dr.SR_NO_LIST, dr.MARKED_DATE, dr.MARKED_BY, dr.REMARKS,
                                       dv.SR_NO, dv.FIRST_NAME, dv.LAST_NAME, dv.RELATION_FIRSTNAME, dv.EPIC_NUMBER
                                FROM DUPLICATION_RECORDS dr 
                                JOIN DUPLICATE_VOTERS dv ON dv.DUPLICATION_ID = dr.DUPLICATION_ID";

                    if ( duplicationId.HasValue )
                    {
                        sql += " WHERE dr.DUPLICATION_ID = :duplicationId";
                    }

                    sql += " ORDER BY dr.DUPLICATION_ID, dv.SR_NO";

                    using (var command = new OracleCommand(sql, connection))
                    {
                        command.CommandTimeout = 30;

                        if ( duplicationId.HasValue )
                        {
                            command.Parameters.Add("duplicationId", OracleDbType.Int32).Value = duplicationId.Value;
                        }

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var dupId = Convert.ToInt32(reader["DUPLICATION_ID"]);

                                if ( !groupDict.ContainsKey(dupId) )
                                {
                                    groupDict[dupId] = new DuplicateGroup
                                    {
                                        DuplicationId = dupId,
                                        SrNoList = reader["SR_NO_LIST"]?.ToString(),
                                        MarkedDate = reader["MARKED_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["MARKED_DATE"]) : (DateTime?)null,
                                        MarkedBy = reader["MARKED_BY"]?.ToString(),
                                        Remarks = reader["REMARKS"]?.ToString(),
                                        Records = new List<VoterRecord>()
                                    };
                                }

                                groupDict[dupId].Records.Add(new VoterRecord
                                {
                                    SrNo = Convert.ToInt32(reader["SR_NO"]),
                                    FirstName = reader["FIRST_NAME"]?.ToString(),
                                    LastName = reader["LAST_NAME"]?.ToString(),
                                    RelationFirstname = reader["RELATION_FIRSTNAME"]?.ToString(),
                                    EpicNumber = reader["EPIC_NUMBER"]?.ToString()
                                });
                            }
                        }
                    }
                }

                response.Groups = groupDict.Values.ToList();
                return response;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error getting duplicate groups: {ex.Message}", ex);
            }
        }

        public async Task<VoterRecord> GetVoterBySrNoAsync(int srNo)
        {
            try
            {
                using (var connection = _connectionFactory.Create())
                {
                    await connection.OpenAsync();

                    var sql = "SELECT * FROM DUPLICATE_VOTERS WHERE SR_NO = :srNo";

                    using (var command = new OracleCommand(sql, connection))
                    {
                        command.CommandTimeout = 30;
                        command.Parameters.Add("srNo", OracleDbType.Int32).Value = srNo;

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return MapVoterRecord((OracleDataReader)reader);
                            }
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error getting voter by SR_NO: {ex.Message}", ex);
            }
        }

        public async Task<ResetVerificationResponse> ResetVerificationAsync()
        {
            try
            {
                using (var connection = _connectionFactory.Create())
                {
                    await connection.OpenAsync();

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            int recordsReset = 0;
                            int groupsDeleted = 0;

                            // Update voters
                            using (var updateCmd = new OracleCommand(
                                "UPDATE DUPLICATE_VOTERS SET DUPLICATE_FLAG='UNKNOWN', DUPLICATION_ID=NULL, VERIFIED='FALSE'", 
                                connection))
                            {
                                updateCmd.Transaction = transaction;
                                updateCmd.CommandTimeout = 60;
                                recordsReset = await updateCmd.ExecuteNonQueryAsync();
                            }

                            // Delete duplication records
                            using (var deleteCmd = new OracleCommand("DELETE FROM DUPLICATION_RECORDS", connection))
                            {
                                deleteCmd.Transaction = transaction;
                                deleteCmd.CommandTimeout = 60;
                                groupsDeleted = await deleteCmd.ExecuteNonQueryAsync();
                            }

                            transaction.Commit();

                            return new ResetVerificationResponse
                            {
                                Status = "SUCCESS",
                                RecordsReset = recordsReset,
                                GroupsDeleted = groupsDeleted
                            };
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error resetting verification: {ex.Message}", ex);
            }
        }

        public async Task<int> GetUnverifiedCountAsync()
        {
            try
            {
                using (var connection = _connectionFactory.Create())
                {
                    await connection.OpenAsync();

                    var sql = "SELECT COUNT(*) FROM DUPLICATE_VOTERS WHERE VERIFIED='FALSE' OR VERIFIED IS NULL";

                    using (var command = new OracleCommand(sql, connection))
                    {
                        command.CommandTimeout = 30;
                        var result = await command.ExecuteScalarAsync();
                        return Convert.ToInt32(result ?? 0);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error getting unverified count: {ex.Message}", ex);
            }
        }

        public async Task<VoterReportResponse> GetVoterReportAsync(VoterReportRequest request)
        {
            try
            {
                var records = new List<VoterReportItem>();
                int totalCount = 0;

                using (var connection = _connectionFactory.Create())
                {
                    await connection.OpenAsync();

                    using (var command = new OracleCommand("PROC_GET_VOTER_REPORT", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 60;

                        // Input parameters (use DBNull for nulls)
                        command.Parameters.Add("P_FIRST_NAME", OracleDbType.Varchar2).Value = (object)request.FirstName ?? DBNull.Value;
                        command.Parameters.Add("P_MIDDLE_NAME", OracleDbType.Varchar2).Value = (object)request.MiddleName ?? DBNull.Value;
                        command.Parameters.Add("P_LAST_NAME", OracleDbType.Varchar2).Value = (object)request.LastName ?? DBNull.Value;
                        command.Parameters.Add("P_WARD_DIV_NO", OracleDbType.Varchar2).Value = (object)request.WardDivNo ?? DBNull.Value;
                        command.Parameters.Add("P_EPIC_NUMBER", OracleDbType.Varchar2).Value = (object)request.EpicNumber ?? DBNull.Value;
                        command.Parameters.Add("P_VOTER_SERIAL_NO", OracleDbType.Varchar2).Value = (object)request.VoterSerialNo ?? DBNull.Value;
                        command.Parameters.Add("P_SEX", OracleDbType.Varchar2).Value = (object)request.Sex ?? DBNull.Value;
                        command.Parameters.Add("P_AGE_MIN", OracleDbType.Decimal).Value = (object)request.AgeMin ?? DBNull.Value;
                        command.Parameters.Add("P_AGE_MAX", OracleDbType.Decimal).Value = (object)request.AgeMax ?? DBNull.Value;
                        command.Parameters.Add("P_DUPLICATE_FLAG", OracleDbType.Varchar2).Value = (object)request.DuplicateFlag ?? DBNull.Value;
                        command.Parameters.Add("P_VERIFIED", OracleDbType.Varchar2).Value = (object)request.Verified ?? DBNull.Value;
                        command.Parameters.Add("P_DUPLICATION_ID", OracleDbType.Decimal).Value = (object)request.DuplicationId ?? DBNull.Value;
                        command.Parameters.Add("P_MARKED_FROM_DATE", OracleDbType.Date).Value = (object)request.MarkedFromDate ?? DBNull.Value;
                        command.Parameters.Add("P_MARKED_TO_DATE", OracleDbType.Date).Value = (object)request.MarkedToDate ?? DBNull.Value;
                        command.Parameters.Add("P_SORT_BY", OracleDbType.Varchar2).Value = (object)(request.SortBy ?? "SR_NO");
                        command.Parameters.Add("P_SORT_DIR", OracleDbType.Varchar2).Value = (object)(request.SortDir ?? "ASC");
                        command.Parameters.Add("P_PAGE_NUMBER", OracleDbType.Decimal).Value = (object)request.PageNumber ?? DBNull.Value;
                        command.Parameters.Add("P_PAGE_SIZE", OracleDbType.Decimal).Value = (object)request.PageSize ?? DBNull.Value;

                        // Output parameters
                        command.Parameters.Add("P_RESULT_CURSOR", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
                        var totalParam = command.Parameters.Add("P_TOTAL_COUNT", OracleDbType.Decimal);
                        totalParam.Direction = ParameterDirection.Output;

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                records.Add(new VoterReportItem
                                {
                                    SrNo = GetInt32OrDefault((OracleDataReader)reader, "SR_NO"),
                                    WardDivNo = GetStringOrDefault((OracleDataReader)reader, "WARD_DIV_NO"),
                                    FirstName = GetStringOrDefault((OracleDataReader)reader, "FIRST_NAME"),
                                    LastName = GetStringOrDefault((OracleDataReader)reader, "LAST_NAME"),
                                    RelationFirstname = GetStringOrDefault((OracleDataReader)reader, "RELATION_FIRSTNAME"),
                                    RelationLastname = GetStringOrDefault((OracleDataReader)reader, "RELATION_LASTNAME"),
                                    RelationType = GetStringOrDefault((OracleDataReader)reader, "RELATION_TYPE"),
                                    HouseNo = GetStringOrDefault((OracleDataReader)reader, "HOUSE_NO"),
                                    Age = GetNullableInt32((OracleDataReader)reader, "AGE"),
                                    Sex = GetStringOrDefault((OracleDataReader)reader, "SEX"),
                                    EpicNumber = GetStringOrDefault((OracleDataReader)reader, "EPIC_NUMBER"),
                                    VoterSerialNo = GetStringOrDefault((OracleDataReader)reader, "VOTER_SERIAL_NO"),
                                    DuplicateFlag = GetStringOrDefault((OracleDataReader)reader, "DUPLICATE_FLAG"),
                                    Verified = GetStringOrDefault((OracleDataReader)reader, "VERIFIED"),
                                    DuplicationId = GetNullableInt32((OracleDataReader)reader, "DUPLICATION_ID"),
                                    MarkedDate = reader["MARKED_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["MARKED_DATE"]) : (DateTime?)null,
                                    MarkedBy = reader["MARKED_BY"]?.ToString(),
                                    Remarks = reader["REMARKS"]?.ToString()
                                });
                            }
                        }

                        var totalVal = totalParam.Value;
                        var total = 0;
                        if (totalVal != null && totalVal != DBNull.Value)
                        {
                            if (totalVal is OracleDecimal od)
                                total = od.IsNull ? 0 : (int)od.Value;
                            else
                                total = Convert.ToInt32(totalVal);
                        }
                        else
                        {
                            total = records.Count;
                        }

                        return VoterReportResponse.CreateSuccess(total, records);
                    }
                }
            }
            catch (OracleException oex)
            {
                var msg = $"Oracle error: {oex.Message}";
                System.Diagnostics.Trace.TraceError(msg);
                return VoterReportResponse.CreateError(msg, "ORACLE_ERROR");
            }
            catch (Exception ex)
            {
                var msg = $"Error generating voter report: {ex.Message}";
                System.Diagnostics.Trace.TraceError(msg);
                return VoterReportResponse.CreateError(msg, "REPORT_ERROR");
            }
        }

        #region Helper Methods

        private VoterRecord MapVoterRecord(OracleDataReader reader)
        {
            return new VoterRecord
            {
                SrNo = GetInt32OrDefault(reader, "SR_NO"),
                WardDivNo = GetStringOrDefault(reader, "WARD_DIV_NO"),
                FirstName = GetStringOrDefault(reader, "FIRST_NAME"),
                LastName = GetStringOrDefault(reader, "LAST_NAME"),
                RelationFirstname = GetStringOrDefault(reader, "RELATION_FIRSTNAME"),
                RelationLastname = GetStringOrDefault(reader, "RELATION_LASTNAME"),
                RelationType = GetStringOrDefault(reader, "RELATION_TYPE"),
                HouseNo = GetStringOrDefault(reader, "HOUSE_NO"),
                Age = GetNullableInt32(reader, "AGE"),
                Sex = GetStringOrDefault(reader, "SEX"),
                EpicNumber = GetStringOrDefault(reader, "EPIC_NUMBER"),
                VoterSerialNo = GetStringOrDefault(reader, "VOTER_SERIAL_NO"),
                DuplicateFlag = GetStringOrDefault(reader, "DUPLICATE_FLAG"),
                Verified = GetStringOrDefault(reader, "VERIFIED"),
                DuplicationId = GetNullableInt32(reader, "DUPLICATION_ID")
            };
        }

        /// <summary>
        /// Safely get string value from reader, returns null if column doesn't exist
        /// </summary>
        private string GetStringOrDefault(OracleDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Safely get int32 value from reader, returns 0 if column doesn't exist
        /// </summary>
        private int GetInt32OrDefault(OracleDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                if (reader.IsDBNull(ordinal)) return 0;
                
                // Handle OracleDecimal
                var value = reader.GetValue(ordinal);
                if (value is OracleDecimal oracleDecimal)
                {
                    return oracleDecimal.IsNull ? 0 : (int)oracleDecimal.Value;
                }
                
                return Convert.ToInt32(value);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Safely get nullable int32 value from reader, returns null if column doesn't exist
        /// </summary>
        private int? GetNullableInt32(OracleDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                if (reader.IsDBNull(ordinal)) return null;
                
                // Handle OracleDecimal
                var value = reader.GetValue(ordinal);
                if (value is OracleDecimal oracleDecimal)
                {
                    return oracleDecimal.IsNull ? (int?)null : (int)oracleDecimal.Value;
                }
                
                return Convert.ToInt32(value);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Convert Oracle output parameter (OracleDecimal) to int
        /// </summary>
        private int ConvertOracleDecimalToInt(object value)
        {
            if (value == null || value == DBNull.Value) return 0;
            
            if (value is OracleDecimal oracleDecimal)
            {
                return oracleDecimal.IsNull ? 0 : (int)oracleDecimal.Value;
            }
            
            return Convert.ToInt32(value);
        }

        /// <summary>
        /// Convert Oracle output parameter (OracleDecimal) to decimal
        /// </summary>
        private decimal ConvertOracleDecimalToDecimal(object value)
        {
            if (value == null || value == DBNull.Value) return 0;
            
            if (value is OracleDecimal oracleDecimal)
            {
                return oracleDecimal.IsNull ? 0 : oracleDecimal.Value;
            }
            
            return Convert.ToDecimal(value);
        }

        #endregion
    }
}

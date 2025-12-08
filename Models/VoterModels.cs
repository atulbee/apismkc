using System;
using System.Collections.Generic;

namespace SmkcApi.Models
{
    #region Request Models

    /// <summary>
    /// Request model for finding duplicate voters
    /// </summary>
    public class FindDuplicatesRequest
    {
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
    }

    /// <summary>
    /// Request model for marking duplicate voters
    /// </summary>
    public class MarkDuplicatesRequest
    {
        public List<int> SrNoArray { get; set; }
        public bool IsDuplicate { get; set; }
        public string Remarks { get; set; }

        public MarkDuplicatesRequest()
        {
            SrNoArray = new List<int>();
        }
    }

    public class VoterReportRequest
    {
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string WardDivNo { get; set; }
        public string EpicNumber { get; set; }
        public string VoterSerialNo { get; set; }
        public string Sex { get; set; }
        public int? AgeMin { get; set; }
        public int? AgeMax { get; set; }
        public string DuplicateFlag { get; set; }
        public string Verified { get; set; }
        public int? DuplicationId { get; set; }
        public DateTime? MarkedFromDate { get; set; }
        public DateTime? MarkedToDate { get; set; }
        public string SortBy { get; set; } = "SR_NO";
        public string SortDir { get; set; } = "ASC";
        public int? PageNumber { get; set; }
        public int? PageSize { get; set; }
    }

    #endregion

    #region Response Models

    /// <summary>
    /// Voter record model
    /// </summary>
    public class VoterRecord
    {
        public int SrNo { get; set; }
        public string WardDivNo { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string RelationFirstname { get; set; }
        public string RelationLastname { get; set; }
        public string RelationType { get; set; }
        public string HouseNo { get; set; }
        public int? Age { get; set; }
        public string Sex { get; set; }
        public string EpicNumber { get; set; }
        public string VoterSerialNo { get; set; }
        public string DuplicateFlag { get; set; }
        public string Verified { get; set; }
        public int? DuplicationId { get; set; }
    }

    /// <summary>
    /// Find duplicates response model
    /// </summary>
    public class FindDuplicatesResponse
    {
        public int DuplicateCount { get; set; }
        public List<VoterRecord> Records { get; set; }

        public FindDuplicatesResponse()
        {
            Records = new List<VoterRecord>();
        }
    }

    /// <summary>
    /// Mark duplicates response model
    /// </summary>
    public class MarkDuplicatesResponse
    {
        public string Status { get; set; }
        public int? DuplicationId { get; set; }
    }

    /// <summary>
    /// Verification status response model
    /// </summary>
    public class VerificationStatusResponse
    {
        public int TotalRecords { get; set; }
        public int VerifiedRecords { get; set; }
        public int UnverifiedRecords { get; set; }
        public int DuplicateRecords { get; set; }
        public int NotDuplicateRecords { get; set; }
        public decimal VerificationPercentage { get; set; }
    }

    /// <summary>
    /// Duplicate group model
    /// </summary>
    public class DuplicateGroup
    {
        public int DuplicationId { get; set; }
        public string SrNoList { get; set; }
        public DateTime? MarkedDate { get; set; }
        public string MarkedBy { get; set; }
        public string Remarks { get; set; }
        public List<VoterRecord> Records { get; set; }

        public DuplicateGroup()
        {
            Records = new List<VoterRecord>();
        }
    }

    /// <summary>
    /// Duplicate groups response model
    /// </summary>
    public class DuplicateGroupsResponse
    {
        public List<DuplicateGroup> Groups { get; set; }

        public DuplicateGroupsResponse()
        {
            Groups = new List<DuplicateGroup>();
        }
    }

    /// <summary>
    /// Reset verification response model
    /// </summary>
    public class ResetVerificationResponse
    {
        public string Status { get; set; }
        public int RecordsReset { get; set; }
        public int GroupsDeleted { get; set; }
    }

    /// <summary>
    /// Unverified count response model
    /// </summary>
    public class UnverifiedCountResponse
    {
        public int UnverifiedCount { get; set; }
    }

    public class VoterReportItem : VoterRecord
    {
        public DateTime? MarkedDate { get; set; }
        public string MarkedBy { get; set; }
        public string Remarks { get; set; }
    }

    public class VoterReportResponse : ApiResponse<object>
    {
        public static VoterReportResponse CreateSuccess(int totalCount, List<VoterReportItem> records)
        {
            return new VoterReportResponse
            {
                Success = true,
                Message = "Voter report retrieved successfully",
                Data = new { totalCount, records },
                Timestamp = DateTime.UtcNow,
                RequestId = Guid.NewGuid().ToString()
            };
        }

        public static VoterReportResponse CreateError(string message, string code)
        {
            return new VoterReportResponse
            {
                Success = false,
                Message = message,
                ErrorCode = code,
                Timestamp = DateTime.UtcNow,
                RequestId = Guid.NewGuid().ToString()
            };
        }
    }

    #endregion
}

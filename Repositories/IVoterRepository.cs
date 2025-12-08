using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmkcApi.Models;

namespace SmkcApi.Repositories
{
    /// <summary>
    /// Interface for Duplicate Voter Repository
    /// Handles all database operations for duplicate voter management
    /// </summary>
    public interface IVoterRepository
    {
        /// <summary>
        /// Find potential duplicate voter records
        /// </summary>
        Task<FindDuplicatesResponse> FindDuplicateVotersAsync(string firstName, string middleName, string lastName);

        /// <summary>
        /// Mark voters as duplicates or not duplicates
        /// </summary>
        Task<MarkDuplicatesResponse> MarkDuplicatesAsync(List<int> srNoArray, bool isDuplicate, string remarks);

        /// <summary>
        /// Get overall verification status and statistics
        /// </summary>
        Task<VerificationStatusResponse> GetVerificationStatusAsync();

        /// <summary>
        /// Get all duplicate groups with their records
        /// </summary>
        Task<DuplicateGroupsResponse> GetDuplicateGroupsAsync(int? duplicationId = null);

        /// <summary>
        /// Get voter record by SR_NO
        /// </summary>
        Task<VoterRecord> GetVoterBySrNoAsync(int srNo);

        /// <summary>
        /// Reset all verification data
        /// </summary>
        Task<ResetVerificationResponse> ResetVerificationAsync();

        /// <summary>
        /// Get count of unverified records
        /// </summary>
        Task<int> GetUnverifiedCountAsync();

        /// <summary>
        /// Get voter report based on criteria
        /// </summary>
        Task<VoterReportResponse> GetVoterReportAsync(VoterReportRequest request);
    }
}

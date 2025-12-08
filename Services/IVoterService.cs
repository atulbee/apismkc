using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmkcApi.Models;

namespace SmkcApi.Services
{
    /// <summary>
    /// Interface for Voter Service
    /// Business logic layer for duplicate voter operations
    /// </summary>
    public interface IVoterService
    {
        /// <summary>
        /// Find potential duplicate voters
        /// </summary>
        Task<ApiResponse<FindDuplicatesResponse>> FindDuplicateVotersAsync(FindDuplicatesRequest request);

        /// <summary>
        /// Mark voters as duplicates or not duplicates
        /// </summary>
        Task<ApiResponse<MarkDuplicatesResponse>> MarkDuplicatesAsync(MarkDuplicatesRequest request);

        /// <summary>
        /// Get verification status and statistics
        /// </summary>
        Task<ApiResponse<VerificationStatusResponse>> GetVerificationStatusAsync();

        /// <summary>
        /// Get all duplicate groups
        /// </summary>
        Task<ApiResponse<DuplicateGroupsResponse>> GetDuplicateGroupsAsync(int? duplicationId = null);

        /// <summary>
        /// Get voter by SR_NO
        /// </summary>
        Task<ApiResponse<VoterRecord>> GetVoterBySrNoAsync(int srNo);

        /// <summary>
        /// Reset all verification data
        /// </summary>
        Task<ApiResponse<ResetVerificationResponse>> ResetVerificationAsync();

        /// <summary>
        /// Get unverified count
        /// </summary>
        Task<ApiResponse<UnverifiedCountResponse>> GetUnverifiedCountAsync();

        /// <summary>
        /// Get voter report
        /// </summary>
        Task<VoterReportResponse> GetVoterReportAsync(VoterReportRequest request);
    }
}

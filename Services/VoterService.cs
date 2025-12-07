using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmkcApi.Models;
using SmkcApi.Repositories;

namespace SmkcApi.Services
{
    /// <summary>
    /// Voter Service Implementation
    /// Handles business logic for duplicate voter management
    /// </summary>
    public class VoterService : IVoterService
    {
        private readonly IVoterRepository _repository;

        public VoterService(IVoterRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<ApiResponse<FindDuplicatesResponse>> FindDuplicateVotersAsync(FindDuplicatesRequest request)
        {
            try
            {
                // Validation
                if (request == null)
                    return ApiResponse<FindDuplicatesResponse>.CreateError("Request body is required", "MISSING_REQUEST");

                // At least one name parameter should be provided
                if (string.IsNullOrWhiteSpace(request.FirstName) && 
                    string.IsNullOrWhiteSpace(request.MiddleName) && 
                    string.IsNullOrWhiteSpace(request.LastName))
                {
                    return ApiResponse<FindDuplicatesResponse>.CreateError(
                        "At least one name parameter (FirstName, MiddleName, or LastName) is required", 
                        "MISSING_NAME_PARAMETER");
                }

                // Call repository
                var result = await _repository.FindDuplicateVotersAsync(
                    request.FirstName?.Trim(),
                    request.MiddleName?.Trim(),
                    request.LastName?.Trim());

                return ApiResponse<FindDuplicatesResponse>.CreateSuccess(
                    result, 
                    $"Found {result.DuplicateCount} potential duplicate records");
            }
            catch (Exception ex)
            {
                return ApiResponse<FindDuplicatesResponse>.CreateError(
                    $"Error finding duplicate voters: {ex.Message}", 
                    "FIND_DUPLICATES_ERROR");
            }
        }

        public async Task<ApiResponse<MarkDuplicatesResponse>> MarkDuplicatesAsync(MarkDuplicatesRequest request)
        {
            try
            {
                // Validation
                if (request == null)
                    return ApiResponse<MarkDuplicatesResponse>.CreateError("Request body is required", "MISSING_REQUEST");

                if (request.SrNoArray == null || !request.SrNoArray.Any())
                    return ApiResponse<MarkDuplicatesResponse>.CreateError(
                        "SrNoArray is required and must contain at least one SR_NO", 
                        "MISSING_SR_NO_ARRAY");

                // Validate that array has at least 2 items for marking as duplicate
                if (request.IsDuplicate && request.SrNoArray.Count < 2)
                    return ApiResponse<MarkDuplicatesResponse>.CreateError(
                        "At least 2 SR_NO values are required to mark as duplicates", 
                        "INSUFFICIENT_SR_NO_COUNT");

                // Validate all SR_NO values are positive
                if (request.SrNoArray.Any(x => x <= 0))
                    return ApiResponse<MarkDuplicatesResponse>.CreateError(
                        "All SR_NO values must be positive integers", 
                        "INVALID_SR_NO_VALUE");

                // Call repository
                var result = await _repository.MarkDuplicatesAsync(
                    request.SrNoArray,
                    request.IsDuplicate,
                    request.Remarks);

                var message = request.IsDuplicate
                    ? $"Successfully marked {request.SrNoArray.Count} voters as duplicates"
                    : $"Successfully marked {request.SrNoArray.Count} voters as not duplicates";

                return ApiResponse<MarkDuplicatesResponse>.CreateSuccess(result, message);
            }
            catch (Exception ex)
            {
                return ApiResponse<MarkDuplicatesResponse>.CreateError(
                    $"Error marking duplicates: {ex.Message}", 
                    "MARK_DUPLICATES_ERROR");
            }
        }

        public async Task<ApiResponse<VerificationStatusResponse>> GetVerificationStatusAsync()
        {
            try
            {
                var result = await _repository.GetVerificationStatusAsync();

                return ApiResponse<VerificationStatusResponse>.CreateSuccess(
                    result, 
                    "Verification status retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<VerificationStatusResponse>.CreateError(
                    $"Error getting verification status: {ex.Message}", 
                    "VERIFICATION_STATUS_ERROR");
            }
        }

        public async Task<ApiResponse<DuplicateGroupsResponse>> GetDuplicateGroupsAsync(int? duplicationId = null)
        {
            try
            {
                // Validate duplication ID if provided
                if (duplicationId.HasValue && duplicationId.Value <= 0)
                    return ApiResponse<DuplicateGroupsResponse>.CreateError(
                        "Duplication ID must be a positive integer", 
                        "INVALID_DUPLICATION_ID");

                var result = await _repository.GetDuplicateGroupsAsync(duplicationId);

                var message = duplicationId.HasValue
                    ? $"Retrieved duplicate group {duplicationId.Value}"
                    : $"Retrieved {result.Groups.Count} duplicate groups";

                return ApiResponse<DuplicateGroupsResponse>.CreateSuccess(result, message);
            }
            catch (Exception ex)
            {
                return ApiResponse<DuplicateGroupsResponse>.CreateError(
                    $"Error getting duplicate groups: {ex.Message}", 
                    "DUPLICATE_GROUPS_ERROR");
            }
        }

        public async Task<ApiResponse<VoterRecord>> GetVoterBySrNoAsync(int srNo)
        {
            try
            {
                // Validation
                if (srNo <= 0)
                    return ApiResponse<VoterRecord>.CreateError(
                        "SR_NO must be a positive integer", 
                        "INVALID_SR_NO");

                var result = await _repository.GetVoterBySrNoAsync(srNo);

                if (result == null)
                    return ApiResponse<VoterRecord>.CreateError(
                        $"Voter with SR_NO {srNo} not found", 
                        "VOTER_NOT_FOUND");

                return ApiResponse<VoterRecord>.CreateSuccess(
                    result, 
                    "Voter record retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<VoterRecord>.CreateError(
                    $"Error getting voter record: {ex.Message}", 
                    "GET_VOTER_ERROR");
            }
        }

        public async Task<ApiResponse<ResetVerificationResponse>> ResetVerificationAsync()
        {
            try
            {
                var result = await _repository.ResetVerificationAsync();

                return ApiResponse<ResetVerificationResponse>.CreateSuccess(
                    result, 
                    $"Verification reset successfully: {result.RecordsReset} records reset, {result.GroupsDeleted} groups deleted");
            }
            catch (Exception ex)
            {
                return ApiResponse<ResetVerificationResponse>.CreateError(
                    $"Error resetting verification: {ex.Message}", 
                    "RESET_VERIFICATION_ERROR");
            }
        }

        public async Task<ApiResponse<UnverifiedCountResponse>> GetUnverifiedCountAsync()
        {
            try
            {
                var count = await _repository.GetUnverifiedCountAsync();

                var result = new UnverifiedCountResponse
                {
                    UnverifiedCount = count
                };

                return ApiResponse<UnverifiedCountResponse>.CreateSuccess(
                    result, 
                    $"Unverified count retrieved successfully: {count} records");
            }
            catch (Exception ex)
            {
                return ApiResponse<UnverifiedCountResponse>.CreateError(
                    $"Error getting unverified count: {ex.Message}", 
                    "UNVERIFIED_COUNT_ERROR");
            }
        }
    }
}

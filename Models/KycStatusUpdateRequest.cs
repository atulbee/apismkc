using System;

namespace SmkcApi.Models
{
    /// <summary>
    /// Request payload for updating KYC status
    /// </summary>
    public class KycStatusUpdateRequest
    {
        public string KycStatus { get; set; }
    }
}

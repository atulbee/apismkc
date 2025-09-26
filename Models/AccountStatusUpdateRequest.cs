using System;

namespace SmkcApi.Models
{
    /// <summary>
    /// Request payload for updating an account status
    /// </summary>
    public class AccountStatusUpdateRequest
    {
        public string Status { get; set; }
    }
}

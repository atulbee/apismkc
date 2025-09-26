using System;

namespace SmkcApi.Models
{
    /// <summary>
    /// Request payload for a transaction reversal
    /// </summary>
    public class TransactionReversalRequest
    {
        public string Reason { get; set; }
    }
}

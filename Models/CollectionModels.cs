using System;

namespace SmkcApi.Models
{
    /// <summary>
    /// Bank → Our API: collection posting payload
    /// </summary>
    public class CollectionPostRequest
    {
        public string ConsumerNo { get; set; }          // Water connection no
        public decimal Amount { get; set; }          // Paid amount
        public string PaymentMode { get; set; }          // UPI|NEFT|RTGS|IMPS|CARD|CHEQUE|CASH
        public string BankTxnId { get; set; }          // Unique bank txn id (idempotency)
        public string UtrNo { get; set; }          // Optional UTR for NEFT/RTGS
        public string PaidDate { get; set; }          // ISO 8601 string, e.g. "2025-10-10T10:43:00Z"
        public string Narration { get; set; }          // Optional note
    }

    /// <summary>
    /// Our API → Bank: response after posting a collection
    /// </summary>
    public class CollectionPostResponse
    {
        public string ReceiptNo { get; set; }         // Generated receipt no
        public string ConsumerNo { get; set; }
        public decimal PostedAmount { get; set; }
        public string PostedAt { get; set; }         // "yyyy-MM-ddTHH:mm:ssZ"
        public string BankTxnId { get; set; }
        public decimal BalanceAfter { get; set; }         // Remaining balance after posting
    }

    public enum CollectionPostStatus
    {
        Success,
        Duplicate,
        NotFound,
        BusinessRuleFailed,
        Error
    }

    /// <summary>
    /// Internal service result used by controller to choose HTTP code.
    /// </summary>
    public class CollectionPostResult
    {
        public CollectionPostStatus Status { get; set; }
        public CollectionPostResponse Payload { get; set; }  // present for Success/Duplicate
        public string ErrorMessage { get; set; }  // for BusinessRuleFailed/Error
    }
}

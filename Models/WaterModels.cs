namespace SmkcApi.Models
{
    public class BillFetchRequest
    {
        public string ConsumerNo { get; set; }
        public string BillPeriodFrom { get; set; } // optional yyyy-MM-dd
        public string BillPeriodTo { get; set; } // optional yyyy-MM-dd
    }

    public class BillFetchResponse
    {
        public string ConsumerNo { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string ConnectionStatus { get; set; }
        public string Mobile { get; set; }
        public decimal Arrears { get; set; }
        public decimal Current { get; set; }
        public decimal LateFee { get; set; }
        public decimal TotalDue { get; set; }
        public string AsOfDate { get; set; }
        public string LastPaymentDate { get; set; }
        public decimal? LastPaymentAmount { get; set; }
    }

    public class CollectionPostRequest
    {
        public string ConsumerNo { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMode { get; set; }   // UPI|NEFT|RTGS|IMPS|CARD|CHEQUE|CASH
        public string BankTxnId { get; set; }     // unique from bank
        public string UtrNo { get; set; }         // optional
        public string PaidDate { get; set; }      // ISO 8601
        public string Narration { get; set; }
    }

    public class CollectionPostResponse
    {
        public string ReceiptNo { get; set; }
        public string ConsumerNo { get; set; }
        public decimal PostedAmount { get; set; }
        public string PostedAt { get; set; }
        public string BankTxnId { get; set; }
        public decimal BalanceAfter { get; set; }
    }

    public enum CollectionPostStatus { Success, Duplicate, NotFound, BusinessRuleFailed, Error }

    public class CollectionPostResult
    {
        public CollectionPostStatus Status { get; set; }
        public CollectionPostResponse Payload { get; set; }
        public string ErrorMessage { get; set; }
    }
}

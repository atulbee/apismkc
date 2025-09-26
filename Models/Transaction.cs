using System;

namespace SmkcApi.Models
{
    public class Transaction
    {
        public string TransactionId { get; set; }
        public string AccountNumber { get; set; }
        public string TransactionType { get; set; } // Credit, Debit, Transfer
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Description { get; set; }
        public string ReferenceNumber { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime ValueDate { get; set; }
        public string Status { get; set; } // Pending, Completed, Failed, Reversed
        public string CounterpartyAccount { get; set; }
        public string CounterpartyName { get; set; }
        public decimal RunningBalance { get; set; }
        public string Channel { get; set; } // API, ATM, Online, Branch
    }

    public class TransactionRequest
    {
        public string FromAccount { get; set; }
        public string ToAccount { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Description { get; set; }
        public string ReferenceNumber { get; set; }
        public DateTime? ValueDate { get; set; }
    }

    public class TransactionResponse
    {
        public string TransactionId { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public DateTime TransactionDate { get; set; }
        public string ReferenceNumber { get; set; }
    }

    public class TransactionHistoryRequest
    {
        public string AccountNumber { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int PageSize { get; set; } = 50;
        public int PageNumber { get; set; } = 1;
        public string TransactionType { get; set; }
    }
}
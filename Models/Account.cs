using System;

namespace SmkcApi.Models
{
    public class Account
    {
        public string AccountNumber { get; set; }
        public string AccountType { get; set; }
        public decimal Balance { get; set; }
        public string Currency { get; set; }
        public string CustomerReference { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public string Status { get; set; } // Active, Inactive, Suspended
        public string BranchCode { get; set; }
    }

    public class AccountRequest
    {
        public string CustomerReference { get; set; }
        public string AccountType { get; set; }
        public string Currency { get; set; }
        public string BranchCode { get; set; }
    }

    public class AccountBalanceResponse
    {
        public string AccountNumber { get; set; }
        public decimal AvailableBalance { get; set; }
        public decimal ActualBalance { get; set; }
        public string Currency { get; set; }
        public DateTime AsOfDate { get; set; }
    }
}
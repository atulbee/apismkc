namespace SmkcApi.Models
{
    public class BillFetchRequest
    {
        public string ConsumerNo { get; set; }
    }

    public class BillFetchResponse
    {
        // IDs & names
        public string ConsumerNo { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }

        // Status (you’re mapping PaymentStatus here)
        public string ConnectionStatus { get; set; }
        public string Mobile { get; set; }   // keep null for now unless you add it to the proc

        // Amounts
        public decimal Arrears { get; set; }           // you set 0m in the service
        public decimal Current { get; set; }           // you map from TotalBalance
        public decimal LateFee { get; set; }           // you map from InterestBalance
        public decimal TotalDue { get; set; }          // you map from TotalBalance

        // Dates
        public string AsOfDate { get; set; }           // "yyyy-MM-dd"
        public string LastPaymentDate { get; set; }    // already formatted in repo
        public decimal? LastPaymentAmount { get; set; }
    }
}

using System;
using System.Threading.Tasks;
using SmkcApi.Models;
using SmkcApi.Repositories;

namespace SmkcApi.Services
{
    public interface IWaterService
    {
        Task<BillFetchResponse> FetchBillAsync(BillFetchRequest req);
        Task<CollectionPostResult> PostCollectionAsync(CollectionPostRequest req, string idempotencyKey);
    }

    public class WaterService : IWaterService
    {
        private readonly IWaterRepository _repo;
        public WaterService(IWaterRepository repo) { _repo = repo; }

        // UPDATED: Use stored procedure GET_CUSTOMER_API via repository
        public async Task<BillFetchResponse> FetchBillAsync(BillFetchRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.ConsumerNo))
                return null;

            // ✅ await the repo call
            var proc = await _repo.GetBillViaProcAsync(req.ConsumerNo);
            if (proc == null || "NOT_FOUND".Equals(proc.ApiStatus, StringComparison.OrdinalIgnoreCase))
                return null;

            // Map DB-shaped dto to API contract
            var resp = new BillFetchResponse
            {
                // IDs & names
                ConsumerNo = proc.ConnectionNumber,
                Name = proc.CustomerName,
                Address = proc.CustomerAddress,

                // Status
                ConnectionStatus = proc.PaymentStatus,  // CURRENT/PAID/OVERDUE/...
                Mobile = null,                          // not provided by proc currently

                // Amounts
                Arrears = 0m,                           // not available separately in proc
                Current = proc.TotalBalance,
                LateFee = proc.InterestBalance,
                TotalDue = proc.TotalBalance,

                // Dates
                AsOfDate = !string.IsNullOrWhiteSpace(proc.QueryDateTime)
                    ? proc.QueryDateTime.Substring(0, 10)   // yyyy-MM-dd from yyyy-MM-ddTHH:mm:ssZ
                    : DateTime.UtcNow.ToString("yyyy-MM-dd"),
                LastPaymentDate = proc.LastPaymentDate,
                LastPaymentAmount = proc.LastPaymentAmount
            };

            return resp;
        }

        public async Task<CollectionPostResult> PostCollectionAsync(CollectionPostRequest req, string idempotencyKey)
        {
            var existing = await _repo.GetReceiptByBankTxnAsync(req.BankTxnId);
            if (existing != null)
                return new CollectionPostResult { Status = CollectionPostStatus.Duplicate, Payload = existing };

            var customer = await _repo.GetCustomerAsync(req.ConsumerNo);
            if (customer == null)
                return new CollectionPostResult { Status = CollectionPostStatus.NotFound };

            if (customer.Status == "BLOCKED")
                return new CollectionPostResult { Status = CollectionPostStatus.BusinessRuleFailed, ErrorMessage = "Connection blocked" };

            var receipt = await _repo.PostCollectionAsync(req, idempotencyKey);
            return new CollectionPostResult { Status = CollectionPostStatus.Success, Payload = receipt };
        }
    }
}

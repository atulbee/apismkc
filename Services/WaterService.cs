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

        public async Task<BillFetchResponse> FetchBillAsync(BillFetchRequest req)
        {
            var cust = await _repo.GetCustomerAsync(req.ConsumerNo);
            if (cust == null) return null;

            var bal = await _repo.GetBalanceAsync(req.ConsumerNo, req.BillPeriodFrom, req.BillPeriodTo);

            return new BillFetchResponse
            {
                ConsumerNo = cust.ConsumerNo,
                Name = cust.Name,
                Address = cust.Address,
                ConnectionStatus = cust.Status,
                Mobile = cust.Mobile,
                Arrears = bal.Arrears,
                Current = bal.Current,
                LateFee = bal.LateFee,
                TotalDue = bal.TotalDue,
                AsOfDate = bal.AsOfDate,
                LastPaymentDate = cust.LastPaymentDate,
                LastPaymentAmount = cust.LastPaymentAmount
            };
        }

        public async Task<CollectionPostResult> PostCollectionAsync(CollectionPostRequest req, string idempotencyKey)
        {
            // Idempotency via bank txn id (and/or header)
            var existing = await _repo.GetReceiptByBankTxnAsync(req.BankTxnId);
            if (existing != null)
                return new CollectionPostResult { Status = CollectionPostStatus.Duplicate, Payload = existing };

            var customer = await _repo.GetCustomerAsync(req.ConsumerNo);
            if (customer == null)
                return new CollectionPostResult { Status = CollectionPostStatus.NotFound };

            // Example business rule
            if (customer.Status == "BLOCKED")
                return new CollectionPostResult { Status = CollectionPostStatus.BusinessRuleFailed, ErrorMessage = "Connection blocked" };

            var receipt = await _repo.PostCollectionAsync(req, idempotencyKey);
            return new CollectionPostResult { Status = CollectionPostStatus.Success, Payload = receipt };
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using SmkcApi.Models;

namespace SmkcApi.Repositories
{
    public interface IWaterRepository
    {
        Task<CustomerDto> GetCustomerAsync(string consumerNo);
        Task<BalanceDto> GetBalanceAsync(string consumerNo, string from, string to);
        Task<CollectionPostResponse> GetReceiptByBankTxnAsync(string bankTxnId);
        Task<CollectionPostResponse> PostCollectionAsync(CollectionPostRequest req, string idemKey);
    }

    public class WaterRepository : IWaterRepository
    {
        private readonly IOracleConnectionFactory _factory;
        public WaterRepository(IOracleConnectionFactory factory) { _factory = factory; }

        public async Task<CustomerDto> GetCustomerAsync(string consumerNo)
        {
            const string sql = @"
                SELECT CONSUMER_NO, NAME, ADDRESS, STATUS, MOBILE,
                       LAST_PAYMENT_DATE, LAST_PAYMENT_AMOUNT
                  FROM WATER_CUSTOMERS
                 WHERE CONSUMER_NO = :p_consumer_no";

            using (var conn = _factory.Create())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("p_consumer_no", consumerNo);
                await conn.OpenAsync();
                using (var dr = await cmd.ExecuteReaderAsync())
                {
                    if (!await dr.ReadAsync()) return null;
                    return new CustomerDto
                    {
                        ConsumerNo = dr["CONSUMER_NO"].ToString(),
                        Name = dr["NAME"].ToString(),
                        Address = dr["ADDRESS"].ToString(),
                        Status = dr["STATUS"].ToString(),
                        Mobile = dr["MOBILE"].ToString(),
                        LastPaymentDate = dr["LAST_PAYMENT_DATE"] == DBNull.Value ? null : Convert.ToDateTime(dr["LAST_PAYMENT_DATE"]).ToString("yyyy-MM-dd"),
                        LastPaymentAmount = dr["LAST_PAYMENT_AMOUNT"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(dr["LAST_PAYMENT_AMOUNT"])
                    };
                }
            }
        }

        public async Task<BalanceDto> GetBalanceAsync(string consumerNo, string from, string to)
        {
            const string sql = @"
                SELECT ARREARS, CURRENT_DUE, LATE_FEE, (ARREARS+CURRENT_DUE+LATE_FEE) TOTAL_DUE,
                       TO_CHAR(SYSDATE, 'YYYY-MM-DD') AS_OF_DATE
                  FROM WATER_BALANCE_VIEW
                 WHERE CONSUMER_NO = :p_consumer_no";

            using (var conn = _factory.Create())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("p_consumer_no", consumerNo);
                await conn.OpenAsync();
                using (var dr = await cmd.ExecuteReaderAsync())
                {
                    if (!await dr.ReadAsync()) return new BalanceDto();
                    return new BalanceDto
                    {
                        Arrears = dr["ARREARS"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["ARREARS"]),
                        Current = dr["CURRENT_DUE"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["CURRENT_DUE"]),
                        LateFee = dr["LATE_FEE"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["LATE_FEE"]),
                        TotalDue = dr["TOTAL_DUE"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["TOTAL_DUE"]),
                        AsOfDate = dr["AS_OF_DATE"].ToString()
                    };
                }
            }
        }

        public async Task<CollectionPostResponse> GetReceiptByBankTxnAsync(string bankTxnId)
        {
            const string sql = @"
                SELECT RECEIPT_NO, CONSUMER_NO, AMOUNT, POSTED_AT, BANK_TXN_ID, BALANCE_AFTER
                  FROM WATER_COLLECTIONS
                 WHERE BANK_TXN_ID = :p_bank_txn_id";

            using (var conn = _factory.Create())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("p_bank_txn_id", bankTxnId);
                await conn.OpenAsync();
                using (var dr = await cmd.ExecuteReaderAsync())
                {
                    if (!await dr.ReadAsync()) return null;
                    return new CollectionPostResponse
                    {
                        ReceiptNo = dr["RECEIPT_NO"].ToString(),
                        ConsumerNo = dr["CONSUMER_NO"].ToString(),
                        PostedAmount = Convert.ToDecimal(dr["AMOUNT"]),
                        PostedAt = Convert.ToDateTime(dr["POSTED_AT"]).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        BankTxnId = dr["BANK_TXN_ID"].ToString(),
                        BalanceAfter = Convert.ToDecimal(dr["BALANCE_AFTER"])
                    };
                }
            }
        }

        public async Task<CollectionPostResponse> PostCollectionAsync(CollectionPostRequest req, string idemKey)
        {
            using (var conn = _factory.Create())
            {
                await conn.OpenAsync();
                using (var tx = conn.BeginTransaction())
                {
                    // 1) insert collection (BANK_TXN_ID should be UNIQUE to enforce idempotency)
                    var insert = new OracleCommand(@"
                        INSERT INTO WATER_COLLECTIONS
                          (RECEIPT_NO, CONSUMER_NO, AMOUNT, POSTED_AT, BANK_TXN_ID, UTR_NO, MODE, NARRATION, IDEMPOTENCY_KEY)
                        VALUES
                          (:p_receipt_no, :p_consumer_no, :p_amount, SYSTIMESTAMP, :p_bank_txn_id, :p_utr, :p_mode, :p_narr, :p_idem_key)",
                        conn);
                    insert.BindByName = true;
                    var receiptNo = await GenerateReceiptAsync(conn);
                    insert.Parameters.Add("p_receipt_no", receiptNo);
                    insert.Parameters.Add("p_consumer_no", req.ConsumerNo);
                    insert.Parameters.Add("p_amount", req.Amount);
                    insert.Parameters.Add("p_bank_txn_id", req.BankTxnId);
                    insert.Parameters.Add("p_utr", (object)req.UtrNo ?? DBNull.Value);
                    insert.Parameters.Add("p_mode", (object)req.PaymentMode ?? "UNKNOWN");
                    insert.Parameters.Add("p_narr", (object)req.Narration ?? DBNull.Value);
                    insert.Parameters.Add("p_idem_key", (object)idemKey ?? DBNull.Value);
                    await insert.ExecuteNonQueryAsync();

                    // 2) update ledger/balance
                    var upd = new OracleCommand(@"
                        UPDATE WATER_LEDGER
                           SET PAID_AMOUNT = NVL(PAID_AMOUNT,0) + :p_amount
                         WHERE CONSUMER_NO = :p_consumer_no", conn);
                    upd.BindByName = true;
                    upd.Parameters.Add("p_amount", req.Amount);
                    upd.Parameters.Add("p_consumer_no", req.ConsumerNo);
                    await upd.ExecuteNonQueryAsync();

                    // 3) fetch balance after
                    var bal = await GetBalanceAsync(req.ConsumerNo, null, null);

                    tx.Commit();

                    return new CollectionPostResponse
                    {
                        ReceiptNo = receiptNo,
                        ConsumerNo = req.ConsumerNo,
                        PostedAmount = req.Amount,
                        PostedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        BankTxnId = req.BankTxnId,
                        BalanceAfter = bal.TotalDue
                    };
                }
            }
        }

        private async Task<string> GenerateReceiptAsync(OracleConnection conn)
        {
            using (var cmd = new OracleCommand("SELECT 'WT-' || TO_CHAR(SYSDATE,'YYYY') || '-' || LPAD(WATER_RCPT_SEQ.NEXTVAL,6,'0') FROM DUAL", conn))
            {
                var o = await cmd.ExecuteScalarAsync();
                return o.ToString();
            }
        }
    }

    // DTOs (repo projections)
    public class CustomerDto
    {
        public string ConsumerNo { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Status { get; set; }
        public string Mobile { get; set; }
        public string LastPaymentDate { get; set; }
        public decimal? LastPaymentAmount { get; set; }
    }
    public class BalanceDto
    {
        public decimal Arrears { get; set; }
        public decimal Current { get; set; }
        public decimal LateFee { get; set; }
        public decimal TotalDue { get; set; }
        public string AsOfDate { get; set; }
    }
}

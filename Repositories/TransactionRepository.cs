using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmkcApi.Models;

namespace SmkcApi.Repositories
{
    /// <summary>
    /// In-memory implementation of Transaction Repository for demonstration purposes.
    /// In production, this should be replaced with proper database implementation.
    /// </summary>
    public class TransactionRepository : ITransactionRepository
    {
        private static readonly ConcurrentDictionary<string, Transaction> _transactions = new ConcurrentDictionary<string, Transaction>();
        
        static TransactionRepository()
        {
            SeedSampleData();
        }

        public async Task<Transaction> GetTransactionAsync(string transactionId)
        {
            await Task.Delay(1); // Simulate async operation
            
            _transactions.TryGetValue(transactionId, out Transaction transaction);
            return transaction;
        }

        public async Task<Transaction> CreateTransactionAsync(Transaction transaction)
        {
            await Task.Delay(1); // Simulate async operation
            
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            if (string.IsNullOrEmpty(transaction.TransactionId))
                throw new ArgumentException("Transaction ID is required");

            if (_transactions.ContainsKey(transaction.TransactionId))
                throw new InvalidOperationException($"Transaction already exists: {transaction.TransactionId}");

            transaction.TransactionDate = DateTime.UtcNow;
            if (transaction.ValueDate == DateTime.MinValue)
                transaction.ValueDate = DateTime.UtcNow;

            _transactions.TryAdd(transaction.TransactionId, transaction);
            return transaction;
        }

        public async Task<bool> UpdateTransactionAsync(Transaction transaction)
        {
            await Task.Delay(1); // Simulate async operation
            
            if (transaction == null)
                return false;

            if (_transactions.TryGetValue(transaction.TransactionId, out Transaction existingTransaction))
            {
                transaction.TransactionDate = existingTransaction.TransactionDate; // Preserve original date
                _transactions.TryUpdate(transaction.TransactionId, transaction, existingTransaction);
                return true;
            }

            return false;
        }

        public async Task<bool> UpdateTransactionStatusAsync(string transactionId, string status)
        {
            await Task.Delay(1); // Simulate async operation
            
            if (_transactions.TryGetValue(transactionId, out Transaction transaction))
            {
                transaction.Status = status;
                return true;
            }

            return false;
        }

        public async Task<List<Transaction>> GetTransactionsByAccountAsync(string accountNumber, DateTime fromDate, DateTime toDate, int pageSize, int pageNumber)
        {
            await Task.Delay(1); // Simulate async operation
            
            var query = _transactions.Values
                .Where(t => (t.AccountNumber == accountNumber || t.CounterpartyAccount == accountNumber))
                .Where(t => t.TransactionDate >= fromDate && t.TransactionDate <= toDate)
                .OrderByDescending(t => t.TransactionDate);

            var skip = (pageNumber - 1) * pageSize;
            return query.Skip(skip).Take(pageSize).ToList();
        }

        public async Task<int> GetTransactionCountByAccountAsync(string accountNumber, DateTime fromDate, DateTime toDate)
        {
            await Task.Delay(1); // Simulate async operation
            
            return _transactions.Values
                .Count(t => (t.AccountNumber == accountNumber || t.CounterpartyAccount == accountNumber) &&
                           t.TransactionDate >= fromDate && t.TransactionDate <= toDate);
        }

        public async Task<List<Transaction>> GetTransactionsByTypeAsync(string transactionType, DateTime fromDate, DateTime toDate)
        {
            await Task.Delay(1); // Simulate async operation
            
            return _transactions.Values
                .Where(t => t.TransactionType.Equals(transactionType, StringComparison.OrdinalIgnoreCase))
                .Where(t => t.TransactionDate >= fromDate && t.TransactionDate <= toDate)
                .OrderByDescending(t => t.TransactionDate)
                .ToList();
        }

        private static void SeedSampleData()
        {
            var sampleTransactions = new[]
            {
                new Transaction
                {
                    TransactionId = "TXN001",
                    AccountNumber = "ACC1234567890",
                    TransactionType = "Credit",
                    Amount = 1000.00m,
                    Currency = "USD",
                    Description = "Salary deposit",
                    ReferenceNumber = "SAL2024001",
                    TransactionDate = DateTime.UtcNow.AddDays(-5),
                    ValueDate = DateTime.UtcNow.AddDays(-5),
                    Status = "Completed",
                    CounterpartyAccount = "EXT001",
                    CounterpartyName = "Employer Inc",
                    RunningBalance = 16000.50m,
                    Channel = "Online"
                },
                new Transaction
                {
                    TransactionId = "TXN002",
                    AccountNumber = "ACC1234567890",
                    TransactionType = "Debit",
                    Amount = 250.00m,
                    Currency = "USD",
                    Description = "ATM withdrawal",
                    ReferenceNumber = "ATM2024001",
                    TransactionDate = DateTime.UtcNow.AddDays(-3),
                    ValueDate = DateTime.UtcNow.AddDays(-3),
                    Status = "Completed",
                    CounterpartyAccount = "",
                    CounterpartyName = "",
                    RunningBalance = 15750.50m,
                    Channel = "ATM"
                },
                new Transaction
                {
                    TransactionId = "TXN003",
                    AccountNumber = "ACC1234567890",
                    TransactionType = "Transfer",
                    Amount = 500.00m,
                    Currency = "USD",
                    Description = "Transfer to savings",
                    ReferenceNumber = "TRF2024001",
                    TransactionDate = DateTime.UtcNow.AddDays(-2),
                    ValueDate = DateTime.UtcNow.AddDays(-2),
                    Status = "Completed",
                    CounterpartyAccount = "ACC2345678901",
                    CounterpartyName = "John Doe - Savings",
                    RunningBalance = 15250.50m,
                    Channel = "API"
                },
                new Transaction
                {
                    TransactionId = "TXN004",
                    AccountNumber = "ACC2345678901",
                    TransactionType = "Credit",
                    Amount = 500.00m,
                    Currency = "USD",
                    Description = "Transfer from checking",
                    ReferenceNumber = "TRF2024001",
                    TransactionDate = DateTime.UtcNow.AddDays(-2),
                    ValueDate = DateTime.UtcNow.AddDays(-2),
                    Status = "Completed",
                    CounterpartyAccount = "ACC1234567890",
                    CounterpartyName = "John Doe - Checking",
                    RunningBalance = 6000.75m,
                    Channel = "API"
                }
            };

            foreach (var transaction in sampleTransactions)
            {
                _transactions.TryAdd(transaction.TransactionId, transaction);
            }
        }

        /// <summary>
        /// Get all transactions for administrative purposes (not exposed via API)
        /// </summary>
        public List<Transaction> GetAllTransactions()
        {
            return _transactions.Values.ToList();
        }

        /// <summary>
        /// Clear all data (for testing purposes only)
        /// </summary>
        public void ClearAllData()
        {
            _transactions.Clear();
            SeedSampleData();
        }
    }
}
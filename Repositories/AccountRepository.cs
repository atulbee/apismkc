using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmkcApi.Models;

namespace SmkcApi.Repositories
{
    /// <summary>
    /// In-memory implementation of Account Repository for demonstration purposes.
    /// In production, this should be replaced with proper database implementation.
    /// </summary>
    public class AccountRepository : IAccountRepository
    {
        private static readonly ConcurrentDictionary<string, Account> _accounts = new ConcurrentDictionary<string, Account>();
        
        static AccountRepository()
        {
            // Initialize with some sample data for demonstration
            SeedSampleData();
        }

        public async Task<Account> GetAccountAsync(string accountNumber)
        {
            await Task.Delay(1); // Simulate async operation
            
            _accounts.TryGetValue(accountNumber, out Account account);
            return account;
        }

        public async Task<List<Account>> GetAccountsByCustomerAsync(string customerReference)
        {
            await Task.Delay(1); // Simulate async operation
            
            return _accounts.Values
                .Where(a => a.CustomerReference == customerReference)
                .OrderBy(a => a.CreatedDate)
                .ToList();
        }

        public async Task<Account> CreateAccountAsync(Account account)
        {
            await Task.Delay(1); // Simulate async operation
            
            if (account == null)
                throw new ArgumentNullException(nameof(account));

            if (string.IsNullOrEmpty(account.AccountNumber))
                throw new ArgumentException("Account number is required");

            if (_accounts.ContainsKey(account.AccountNumber))
                throw new InvalidOperationException($"Account already exists: {account.AccountNumber}");

            account.CreatedDate = DateTime.UtcNow;
            account.LastModifiedDate = DateTime.UtcNow;

            _accounts.TryAdd(account.AccountNumber, account);
            return account;
        }

        public async Task<bool> UpdateAccountAsync(Account account)
        {
            await Task.Delay(1); // Simulate async operation
            
            if (account == null)
                return false;

            if (_accounts.TryGetValue(account.AccountNumber, out Account existingAccount))
            {
                account.LastModifiedDate = DateTime.UtcNow;
                account.CreatedDate = existingAccount.CreatedDate; // Preserve creation date
                
                _accounts.TryUpdate(account.AccountNumber, account, existingAccount);
                return true;
            }

            return false;
        }

        public async Task<bool> UpdateAccountStatusAsync(string accountNumber, string status)
        {
            await Task.Delay(1); // Simulate async operation
            
            if (_accounts.TryGetValue(accountNumber, out Account account))
            {
                account.Status = status;
                account.LastModifiedDate = DateTime.UtcNow;
                return true;
            }

            return false;
        }

        public async Task<bool> UpdateAccountBalanceAsync(string accountNumber, decimal newBalance)
        {
            await Task.Delay(1); // Simulate async operation
            
            if (_accounts.TryGetValue(accountNumber, out Account account))
            {
                account.Balance = newBalance;
                account.LastModifiedDate = DateTime.UtcNow;
                return true;
            }

            return false;
        }

        public async Task<bool> DeleteAccountAsync(string accountNumber)
        {
            await Task.Delay(1); // Simulate async operation
            
            return _accounts.TryRemove(accountNumber, out _);
        }

        private static void SeedSampleData()
        {
            var sampleAccounts = new[]
            {
                new Account
                {
                    AccountNumber = "ACC1234567890",
                    AccountType = "Savings",
                    Balance = 15000.50m,
                    Currency = "USD",
                    CustomerReference = "CUST001",
                    CreatedDate = DateTime.UtcNow.AddMonths(-6),
                    LastModifiedDate = DateTime.UtcNow.AddDays(-5),
                    Status = "Active",
                    BranchCode = "001"
                },
                new Account
                {
                    AccountNumber = "ACC2345678901",
                    AccountType = "Checking",
                    Balance = 5500.75m,
                    Currency = "USD",
                    CustomerReference = "CUST001",
                    CreatedDate = DateTime.UtcNow.AddMonths(-4),
                    LastModifiedDate = DateTime.UtcNow.AddDays(-2),
                    Status = "Active",
                    BranchCode = "001"
                },
                new Account
                {
                    AccountNumber = "ACC3456789012",
                    AccountType = "Business",
                    Balance = 125000.00m,
                    Currency = "USD",
                    CustomerReference = "CUST002",
                    CreatedDate = DateTime.UtcNow.AddMonths(-12),
                    LastModifiedDate = DateTime.UtcNow.AddDays(-1),
                    Status = "Active",
                    BranchCode = "002"
                },
                new Account
                {
                    AccountNumber = "ACC4567890123",
                    AccountType = "Savings",
                    Balance = 0.00m,
                    Currency = "USD",
                    CustomerReference = "CUST003",
                    CreatedDate = DateTime.UtcNow.AddDays(-30),
                    LastModifiedDate = DateTime.UtcNow.AddDays(-30),
                    Status = "Inactive",
                    BranchCode = "003"
                }
            };

            foreach (var account in sampleAccounts)
            {
                _accounts.TryAdd(account.AccountNumber, account);
            }
        }

        /// <summary>
        /// Get all accounts for administrative purposes (not exposed via API)
        /// </summary>
        public List<Account> GetAllAccounts()
        {
            return _accounts.Values.ToList();
        }

        /// <summary>
        /// Clear all data (for testing purposes only)
        /// </summary>
        public void ClearAllData()
        {
            _accounts.Clear();
            SeedSampleData();
        }
    }
}
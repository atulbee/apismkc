using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmkcApi.Models;
using SmkcApi.Repositories;

namespace SmkcApi.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly ICustomerRepository _customerRepository;

        public AccountService(IAccountRepository accountRepository, ICustomerRepository customerRepository)
        {
            _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
            _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        }

        public async Task<Account> GetAccountAsync(string accountNumber)
        {
            if (string.IsNullOrEmpty(accountNumber))
                throw new ArgumentException("Account number is required", nameof(accountNumber));

            try
            {
                var account = await _accountRepository.GetAccountAsync(accountNumber);
                
                if (account == null)
                {
                    LogEvent($"Account not found: {accountNumber}");
                    return null;
                }

                LogEvent($"Account retrieved successfully: {accountNumber}");
                return account;
            }
            catch (Exception ex)
            {
                LogEvent($"Error retrieving account {accountNumber}: {ex.Message}");
                throw new Exception($"Failed to retrieve account: {ex.Message}");
            }
        }

        public async Task<AccountBalanceResponse> GetAccountBalanceAsync(string accountNumber)
        {
            if (string.IsNullOrEmpty(accountNumber))
                throw new ArgumentException("Account number is required", nameof(accountNumber));

            try
            {
                var account = await _accountRepository.GetAccountAsync(accountNumber);
                
                if (account == null)
                {
                    LogEvent($"Account not found for balance inquiry: {accountNumber}");
                    return null;
                }

                // In a real implementation, you might have separate available and actual balances
                var balanceResponse = new AccountBalanceResponse
                {
                    AccountNumber = account.AccountNumber,
                    AvailableBalance = account.Balance,
                    ActualBalance = account.Balance,
                    Currency = account.Currency,
                    AsOfDate = DateTime.UtcNow
                };

                LogEvent($"Account balance retrieved: {accountNumber}");
                return balanceResponse;
            }
            catch (Exception ex)
            {
                LogEvent($"Error retrieving balance for account {accountNumber}: {ex.Message}");
                throw new Exception($"Failed to retrieve account balance: {ex.Message}");
            }
        }

        public async Task<List<Account>> GetAccountsByCustomerAsync(string customerReference)
        {
            if (string.IsNullOrEmpty(customerReference))
                throw new ArgumentException("Customer reference is required", nameof(customerReference));

            try
            {
                // Validate customer exists
                var customer = await _customerRepository.GetCustomerAsync(customerReference);
                if (customer == null)
                {
                    LogEvent($"Customer not found: {customerReference}");
                    return new List<Account>();
                }

                var accounts = await _accountRepository.GetAccountsByCustomerAsync(customerReference);
                
                LogEvent($"Retrieved {accounts?.Count ?? 0} accounts for customer: {customerReference}");
                return accounts ?? new List<Account>();
            }
            catch (Exception ex)
            {
                LogEvent($"Error retrieving accounts for customer {customerReference}: {ex.Message}");
                throw new Exception($"Failed to retrieve customer accounts: {ex.Message}");
            }
        }

        public async Task<Account> CreateAccountAsync(AccountRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrEmpty(request.CustomerReference))
                throw new ArgumentException("Customer reference is required", nameof(request.CustomerReference));

            if (string.IsNullOrEmpty(request.AccountType))
                throw new ArgumentException("Account type is required", nameof(request.AccountType));

            try
            {
                // Validate customer exists
                var customer = await _customerRepository.GetCustomerAsync(request.CustomerReference);
                if (customer == null)
                {
                    throw new ArgumentException($"Customer not found: {request.CustomerReference}");
                }

                // Create new account
                var account = new Account
                {
                    AccountNumber = GenerateAccountNumber(),
                    AccountType = request.AccountType,
                    Balance = 0.00m, // New accounts start with zero balance
                    Currency = request.Currency ?? "USD",
                    CustomerReference = request.CustomerReference,
                    CreatedDate = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow,
                    Status = "Active",
                    BranchCode = request.BranchCode ?? "001"
                };

                var createdAccount = await _accountRepository.CreateAccountAsync(account);
                
                LogEvent($"Account created successfully: {createdAccount.AccountNumber} for customer: {request.CustomerReference}");
                return createdAccount;
            }
            catch (Exception ex)
            {
                LogEvent($"Error creating account for customer {request.CustomerReference}: {ex.Message}");
                throw new Exception($"Failed to create account: {ex.Message}");
            }
        }

        public async Task<bool> UpdateAccountStatusAsync(string accountNumber, string status)
        {
            if (string.IsNullOrEmpty(accountNumber))
                throw new ArgumentException("Account number is required", nameof(accountNumber));

            if (string.IsNullOrEmpty(status))
                throw new ArgumentException("Status is required", nameof(status));

            var validStatuses = new[] { "Active", "Inactive", "Suspended", "Closed" };
            if (!validStatuses.Contains(status))
                throw new ArgumentException("Invalid status value", nameof(status));

            try
            {
                var result = await _accountRepository.UpdateAccountStatusAsync(accountNumber, status);
                
                if (result)
                {
                    LogEvent($"Account status updated: {accountNumber} to {status}");
                }
                else
                {
                    LogEvent($"Failed to update account status: {accountNumber}");
                }

                return result;
            }
            catch (Exception ex)
            {
                LogEvent($"Error updating account status {accountNumber}: {ex.Message}");
                throw new Exception($"Failed to update account status: {ex.Message}");
            }
        }

        public async Task<bool> ValidateAccountAsync(string accountNumber)
        {
            if (string.IsNullOrEmpty(accountNumber))
                return false;

            try
            {
                var account = await _accountRepository.GetAccountAsync(accountNumber);
                var isValid = account != null && account.Status == "Active";
                
                LogEvent($"Account validation: {accountNumber} - {(isValid ? "Valid" : "Invalid")}");
                return isValid;
            }
            catch (Exception ex)
            {
                LogEvent($"Error validating account {accountNumber}: {ex.Message}");
                return false;
            }
        }

        private string GenerateAccountNumber()
        {
            // Generate a 12-digit account number with check digit
            var random = new Random();
            var accountBase = random.Next(100000000, 999999999).ToString(); // 9 digits
            var checkDigit = CalculateCheckDigit(accountBase);
            
            return $"ACC{accountBase}{checkDigit}";
        }

        private int CalculateCheckDigit(string accountBase)
        {
            // Simple Luhn algorithm implementation
            var sum = 0;
            var alternate = false;
            
            for (int i = accountBase.Length - 1; i >= 0; i--)
            {
                var digit = int.Parse(accountBase[i].ToString());
                
                if (alternate)
                {
                    digit *= 2;
                    if (digit > 9)
                        digit = digit / 10 + digit % 10;
                }
                
                sum += digit;
                alternate = !alternate;
            }
            
            return (10 - (sum % 10)) % 10;
        }

        private void LogEvent(string message)
        {
            var logEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - ACCOUNT_SERVICE: {message}";
            System.Diagnostics.Trace.TraceInformation(logEntry);
        }
    }
}
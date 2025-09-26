using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmkcApi.Models;
using SmkcApi.Repositories;
using SmkcApi.Security;

namespace SmkcApi.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IAccountRepository _accountRepository;

        public TransactionService(ITransactionRepository transactionRepository, IAccountRepository accountRepository)
        {
            _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
            _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        }

        public async Task<TransactionResponse> ProcessTransactionAsync(TransactionRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrEmpty(request.FromAccount))
                throw new ArgumentException("From account is required", nameof(request.FromAccount));

            if (string.IsNullOrEmpty(request.ToAccount))
                throw new ArgumentException("To account is required", nameof(request.ToAccount));

            if (request.Amount <= 0)
                throw new ArgumentException("Amount must be greater than zero", nameof(request.Amount));

            try
            {
                // Validate transaction
                var isValid = await ValidateTransactionAsync(request);
                if (!isValid)
                {
                    return new TransactionResponse
                    {
                        Status = "Failed",
                        Message = "Transaction validation failed",
                        TransactionDate = DateTime.UtcNow,
                        ReferenceNumber = request.ReferenceNumber
                    };
                }

                var transactionId = GenerateTransactionId();

                // Create transaction record
                var transaction = new Transaction
                {
                    TransactionId = transactionId,
                    AccountNumber = request.FromAccount,
                    TransactionType = "Transfer",
                    Amount = request.Amount,
                    Currency = request.Currency ?? "USD",
                    Description = request.Description ?? "Transfer",
                    ReferenceNumber = request.ReferenceNumber ?? GenerateReferenceNumber(),
                    TransactionDate = DateTime.UtcNow,
                    ValueDate = request.ValueDate ?? DateTime.UtcNow,
                    Status = "Pending",
                    CounterpartyAccount = request.ToAccount,
                    CounterpartyName = await GetAccountHolderName(request.ToAccount),
                    Channel = "API"
                };

                // Process the transaction (simulate)
                var success = await ProcessTransactionLogic(request, transaction);
                
                transaction.Status = success ? "Completed" : "Failed";
                
                // Save transaction
                await _transactionRepository.CreateTransactionAsync(transaction);

                LogEvent($"Transaction processed: {transactionId} - Status: {transaction.Status}");

                return new TransactionResponse
                {
                    TransactionId = transactionId,
                    Status = transaction.Status,
                    Message = success ? "Transaction processed successfully" : "Transaction processing failed",
                    TransactionDate = transaction.TransactionDate,
                    ReferenceNumber = transaction.ReferenceNumber
                };
            }
            catch (Exception ex)
            {
                LogEvent($"Error processing transaction: {ex.Message}");
                throw new Exception($"Failed to process transaction: {ex.Message}");
            }
        }

        public async Task<Transaction> GetTransactionAsync(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId))
                throw new ArgumentException("Transaction ID is required", nameof(transactionId));

            try
            {
                var transaction = await _transactionRepository.GetTransactionAsync(transactionId);
                
                if (transaction == null)
                {
                    LogEvent($"Transaction not found: {transactionId}");
                    return null;
                }

                LogEvent($"Transaction retrieved successfully: {transactionId}");
                return transaction;
            }
            catch (Exception ex)
            {
                LogEvent($"Error retrieving transaction {transactionId}: {ex.Message}");
                throw new Exception($"Failed to retrieve transaction: {ex.Message}");
            }
        }

        public async Task<PagedResult<Transaction>> GetTransactionHistoryAsync(TransactionHistoryRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrEmpty(request.AccountNumber))
                throw new ArgumentException("Account number is required", nameof(request.AccountNumber));

            if (request.FromDate >= request.ToDate)
                throw new ArgumentException("From date must be before to date");

            try
            {
                var transactions = await _transactionRepository.GetTransactionsByAccountAsync(
                    request.AccountNumber, 
                    request.FromDate, 
                    request.ToDate, 
                    request.PageSize, 
                    request.PageNumber);

                var totalCount = await _transactionRepository.GetTransactionCountByAccountAsync(
                    request.AccountNumber, 
                    request.FromDate, 
                    request.ToDate);

                var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

                var result = new PagedResult<Transaction>
                {
                    Data = transactions,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalPages = totalPages,
                    HasNextPage = request.PageNumber < totalPages,
                    HasPreviousPage = request.PageNumber > 1
                };

                LogEvent($"Transaction history retrieved for account {request.AccountNumber}: {transactions.Count} transactions");
                return result;
            }
            catch (Exception ex)
            {
                LogEvent($"Error retrieving transaction history for account {request.AccountNumber}: {ex.Message}");
                throw new Exception($"Failed to retrieve transaction history: {ex.Message}");
            }
        }

        public async Task<TransactionResponse> ReverseTransactionAsync(string transactionId, string reason)
        {
            if (string.IsNullOrEmpty(transactionId))
                throw new ArgumentException("Transaction ID is required", nameof(transactionId));

            if (string.IsNullOrEmpty(reason))
                throw new ArgumentException("Reversal reason is required", nameof(reason));

            try
            {
                var originalTransaction = await _transactionRepository.GetTransactionAsync(transactionId);
                
                if (originalTransaction == null)
                {
                    return new TransactionResponse
                    {
                        Status = "Failed",
                        Message = "Original transaction not found",
                        TransactionDate = DateTime.UtcNow
                    };
                }

                if (originalTransaction.Status != "Completed")
                {
                    return new TransactionResponse
                    {
                        Status = "Failed",
                        Message = "Can only reverse completed transactions",
                        TransactionDate = DateTime.UtcNow
                    };
                }

                var reversalId = GenerateTransactionId();

                // Create reversal transaction
                var reversalTransaction = new Transaction
                {
                    TransactionId = reversalId,
                    AccountNumber = originalTransaction.CounterpartyAccount, // Reverse the accounts
                    TransactionType = "Reversal",
                    Amount = originalTransaction.Amount,
                    Currency = originalTransaction.Currency,
                    Description = $"Reversal of {originalTransaction.TransactionId} - {reason}",
                    ReferenceNumber = GenerateReferenceNumber(),
                    TransactionDate = DateTime.UtcNow,
                    ValueDate = DateTime.UtcNow,
                    Status = "Completed",
                    CounterpartyAccount = originalTransaction.AccountNumber,
                    CounterpartyName = await GetAccountHolderName(originalTransaction.AccountNumber),
                    Channel = "API"
                };

                // Save reversal transaction
                await _transactionRepository.CreateTransactionAsync(reversalTransaction);

                // Update original transaction status
                await _transactionRepository.UpdateTransactionStatusAsync(transactionId, "Reversed");

                LogEvent($"Transaction reversed: {transactionId} with reversal ID: {reversalId}");

                return new TransactionResponse
                {
                    TransactionId = reversalId,
                    Status = "Completed",
                    Message = "Transaction reversal processed successfully",
                    TransactionDate = reversalTransaction.TransactionDate,
                    ReferenceNumber = reversalTransaction.ReferenceNumber
                };
            }
            catch (Exception ex)
            {
                LogEvent($"Error reversing transaction {transactionId}: {ex.Message}");
                throw new Exception($"Failed to reverse transaction: {ex.Message}");
            }
        }

        public async Task<bool> ValidateTransactionAsync(TransactionRequest request)
        {
            if (request == null)
                return false;

            try
            {
                // Validate from account exists and is active
                var fromAccount = await _accountRepository.GetAccountAsync(request.FromAccount);
                if (fromAccount == null || fromAccount.Status != "Active")
                {
                    LogEvent($"Invalid from account: {request.FromAccount}");
                    return false;
                }

                // Validate to account exists and is active
                var toAccount = await _accountRepository.GetAccountAsync(request.ToAccount);
                if (toAccount == null || toAccount.Status != "Active")
                {
                    LogEvent($"Invalid to account: {request.ToAccount}");
                    return false;
                }

                // Validate sufficient balance
                if (fromAccount.Balance < request.Amount)
                {
                    LogEvent($"Insufficient balance for transaction: {request.FromAccount}");
                    return false;
                }

                // Validate currency match
                if (!string.IsNullOrEmpty(request.Currency) && fromAccount.Currency != request.Currency)
                {
                    LogEvent($"Currency mismatch for transaction: {request.FromAccount}");
                    return false;
                }

                LogEvent($"Transaction validation passed for {request.FromAccount} to {request.ToAccount}");
                return true;
            }
            catch (Exception ex)
            {
                LogEvent($"Error validating transaction: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> ProcessTransactionLogic(TransactionRequest request, Transaction transaction)
        {
            try
            {
                // Get account balances
                var fromAccount = await _accountRepository.GetAccountAsync(request.FromAccount);
                var toAccount = await _accountRepository.GetAccountAsync(request.ToAccount);

                // Update balances (simulate atomic transaction)
                var newFromBalance = fromAccount.Balance - request.Amount;
                var newToBalance = toAccount.Balance + request.Amount;

                // Update running balance in transaction record
                transaction.RunningBalance = newFromBalance;

                // Update account balances
                await _accountRepository.UpdateAccountBalanceAsync(request.FromAccount, newFromBalance);
                await _accountRepository.UpdateAccountBalanceAsync(request.ToAccount, newToBalance);

                LogEvent($"Balance updated - From: {request.FromAccount} ({fromAccount.Balance} -> {newFromBalance}), To: {request.ToAccount} ({toAccount.Balance} -> {newToBalance})");
                return true;
            }
            catch (Exception ex)
            {
                LogEvent($"Error processing transaction logic: {ex.Message}");
                return false;
            }
        }

        private async Task<string> GetAccountHolderName(string accountNumber)
        {
            try
            {
                var account = await _accountRepository.GetAccountAsync(accountNumber);
                if (account != null)
                {
                    // In a real system, you would get the customer name
                    return $"Account Holder ({accountNumber})";
                }
                return "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        private string GenerateTransactionId()
        {
            // Generate a transaction ID: TXN + timestamp + random
            var timestamp = SecurityHelper.ToUnixTimeSeconds(DateTimeOffset.UtcNow);
            var random = new Random().Next(1000, 9999);
            return $"TXN{timestamp}{random}";
        }

        private string GenerateReferenceNumber()
        {
            // Generate a reference number: REF + date + random
            var date = DateTime.UtcNow.ToString("yyyyMMdd");
            var random = new Random().Next(100000, 999999);
            return $"REF{date}{random}";
        }

        private void LogEvent(string message)
        {
            var logEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - TRANSACTION_SERVICE: {message}";
            System.Diagnostics.Trace.TraceInformation(logEntry);
        }
    }
}
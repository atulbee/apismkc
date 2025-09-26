using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmkcApi.Models;

namespace SmkcApi.Services
{
    public interface ITransactionService
    {
        Task<TransactionResponse> ProcessTransactionAsync(TransactionRequest request);
        Task<Transaction> GetTransactionAsync(string transactionId);
        Task<PagedResult<Transaction>> GetTransactionHistoryAsync(TransactionHistoryRequest request);
        Task<TransactionResponse> ReverseTransactionAsync(string transactionId, string reason);
        Task<bool> ValidateTransactionAsync(TransactionRequest request);
    }
}
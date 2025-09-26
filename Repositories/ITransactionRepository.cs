using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmkcApi.Models;

namespace SmkcApi.Repositories
{
    public interface ITransactionRepository
    {
        Task<Transaction> GetTransactionAsync(string transactionId);
        Task<Transaction> CreateTransactionAsync(Transaction transaction);
        Task<bool> UpdateTransactionAsync(Transaction transaction);
        Task<bool> UpdateTransactionStatusAsync(string transactionId, string status);
        Task<List<Transaction>> GetTransactionsByAccountAsync(string accountNumber, DateTime fromDate, DateTime toDate, int pageSize, int pageNumber);
        Task<int> GetTransactionCountByAccountAsync(string accountNumber, DateTime fromDate, DateTime toDate);
        Task<List<Transaction>> GetTransactionsByTypeAsync(string transactionType, DateTime fromDate, DateTime toDate);
    }
}
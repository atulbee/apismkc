using System.Collections.Generic;
using System.Threading.Tasks;
using SmkcApi.Models;

namespace SmkcApi.Repositories
{
    public interface IAccountRepository
    {
        Task<Account> GetAccountAsync(string accountNumber);
        Task<List<Account>> GetAccountsByCustomerAsync(string customerReference);
        Task<Account> CreateAccountAsync(Account account);
        Task<bool> UpdateAccountAsync(Account account);
        Task<bool> UpdateAccountStatusAsync(string accountNumber, string status);
        Task<bool> UpdateAccountBalanceAsync(string accountNumber, decimal newBalance);
        Task<bool> DeleteAccountAsync(string accountNumber);
    }
}
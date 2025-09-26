using System.Collections.Generic;
using System.Threading.Tasks;
using SmkcApi.Models;

namespace SmkcApi.Services
{
    public interface IAccountService
    {
        Task<Account> GetAccountAsync(string accountNumber);
        Task<AccountBalanceResponse> GetAccountBalanceAsync(string accountNumber);
        Task<List<Account>> GetAccountsByCustomerAsync(string customerReference);
        Task<Account> CreateAccountAsync(AccountRequest request);
        Task<bool> UpdateAccountStatusAsync(string accountNumber, string status);
        Task<bool> ValidateAccountAsync(string accountNumber);
    }
}
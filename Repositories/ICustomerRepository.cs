using System.Collections.Generic;
using System.Threading.Tasks;
using SmkcApi.Models;

namespace SmkcApi.Repositories
{
    public interface ICustomerRepository
    {
        Task<Customer> GetCustomerAsync(string customerReference);
        Task<Customer> CreateCustomerAsync(Customer customer);
        Task<Customer> UpdateCustomerAsync(Customer customer);
        Task<bool> DeleteCustomerAsync(string customerReference);
        Task<List<Customer>> GetCustomersByStatusAsync(string status);
        Task<bool> UpdateKycStatusAsync(string customerReference, string kycStatus);
    }
}
using System;
using System.Threading.Tasks;
using SmkcApi.Models;

namespace SmkcApi.Services
{
    public interface ICustomerService
    {
        Task<Customer> GetCustomerAsync(string customerReference);
        Task<Customer> CreateCustomerAsync(CustomerRequest request);
        Task<Customer> UpdateCustomerAsync(CustomerUpdateRequest request);
        Task<bool> ValidateCustomerAsync(string customerReference);
        Task<bool> UpdateKycStatusAsync(string customerReference, string kycStatus);
    }
}
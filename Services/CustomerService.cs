using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmkcApi.Models;
using SmkcApi.Repositories;

namespace SmkcApi.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;

        public CustomerService(ICustomerRepository customerRepository)
        {
            _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        }

        public async Task<Customer> GetCustomerAsync(string customerReference)
        {
            if (string.IsNullOrEmpty(customerReference))
                throw new ArgumentException("Customer reference is required", nameof(customerReference));

            try
            {
                var customer = await _customerRepository.GetCustomerAsync(customerReference);
                
                if (customer == null)
                {
                    LogEvent($"Customer not found: {customerReference}");
                    return null;
                }

                LogEvent($"Customer retrieved successfully: {customerReference}");
                return customer;
            }
            catch (Exception ex)
            {
                LogEvent($"Error retrieving customer {customerReference}: {ex.Message}");
                throw new Exception($"Failed to retrieve customer: {ex.Message}");
            }
        }

        public async Task<Customer> CreateCustomerAsync(CustomerRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrEmpty(request.FirstName))
                throw new ArgumentException("First name is required", nameof(request.FirstName));

            if (string.IsNullOrEmpty(request.LastName))
                throw new ArgumentException("Last name is required", nameof(request.LastName));

            if (string.IsNullOrEmpty(request.Email))
                throw new ArgumentException("Email is required", nameof(request.Email));

            if (string.IsNullOrEmpty(request.NationalId))
                throw new ArgumentException("National ID is required", nameof(request.NationalId));

            try
            {
                var customer = new Customer
                {
                    CustomerReference = GenerateCustomerReference(),
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    DateOfBirth = request.DateOfBirth,
                    NationalId = request.NationalId,
                    Address = request.Address,
                    Status = "Active",
                    CreatedDate = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow,
                    KycStatus = "Pending"
                };

                var createdCustomer = await _customerRepository.CreateCustomerAsync(customer);
                
                LogEvent($"Customer created successfully: {createdCustomer.CustomerReference}");
                return createdCustomer;
            }
            catch (Exception ex)
            {
                LogEvent($"Error creating customer: {ex.Message}");
                throw new Exception($"Failed to create customer: {ex.Message}");
            }
        }

        public async Task<Customer> UpdateCustomerAsync(CustomerUpdateRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrEmpty(request.CustomerReference))
                throw new ArgumentException("Customer reference is required", nameof(request.CustomerReference));

            try
            {
                var existingCustomer = await _customerRepository.GetCustomerAsync(request.CustomerReference);
                if (existingCustomer == null)
                {
                    LogEvent($"Customer not found for update: {request.CustomerReference}");
                    return null;
                }

                // Update only the fields that are provided
                if (!string.IsNullOrEmpty(request.Email))
                    existingCustomer.Email = request.Email;
                
                if (!string.IsNullOrEmpty(request.PhoneNumber))
                    existingCustomer.PhoneNumber = request.PhoneNumber;
                
                if (request.Address != null)
                    existingCustomer.Address = request.Address;

                existingCustomer.LastModifiedDate = DateTime.UtcNow;

                var updatedCustomer = await _customerRepository.UpdateCustomerAsync(existingCustomer);
                
                LogEvent($"Customer updated successfully: {request.CustomerReference}");
                return updatedCustomer;
            }
            catch (Exception ex)
            {
                LogEvent($"Error updating customer {request.CustomerReference}: {ex.Message}");
                throw new Exception($"Failed to update customer: {ex.Message}");
            }
        }

        public async Task<bool> ValidateCustomerAsync(string customerReference)
        {
            if (string.IsNullOrEmpty(customerReference))
                return false;

            try
            {
                var customer = await _customerRepository.GetCustomerAsync(customerReference);
                var isValid = customer != null && customer.Status == "Active";
                
                LogEvent($"Customer validation: {customerReference} - {(isValid ? "Valid" : "Invalid")}");
                return isValid;
            }
            catch (Exception ex)
            {
                LogEvent($"Error validating customer {customerReference}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateKycStatusAsync(string customerReference, string kycStatus)
        {
            if (string.IsNullOrEmpty(customerReference))
                throw new ArgumentException("Customer reference is required", nameof(customerReference));

            if (string.IsNullOrEmpty(kycStatus))
                throw new ArgumentException("KYC status is required", nameof(kycStatus));

            var validStatuses = new[] { "Pending", "Verified", "Rejected", "Expired" };
            if (!validStatuses.Contains(kycStatus))
                throw new ArgumentException("Invalid KYC status value", nameof(kycStatus));

            try
            {
                var result = await _customerRepository.UpdateKycStatusAsync(customerReference, kycStatus);
                
                if (result)
                {
                    LogEvent($"Customer KYC status updated: {customerReference} to {kycStatus}");
                }
                else
                {
                    LogEvent($"Failed to update customer KYC status: {customerReference}");
                }

                return result;
            }
            catch (Exception ex)
            {
                LogEvent($"Error updating customer KYC status {customerReference}: {ex.Message}");
                throw new Exception($"Failed to update KYC status: {ex.Message}");
            }
        }

        private string GenerateCustomerReference()
        {
            // Generate a customer reference in format: CUST + 6 digits + check digit
            var random = new Random();
            var customerBase = random.Next(100000, 999999).ToString(); // 6 digits
            var checkDigit = CalculateCheckDigit(customerBase);
            
            return $"CUST{customerBase}{checkDigit}";
        }

        private int CalculateCheckDigit(string customerBase)
        {
            // Simple checksum calculation
            var sum = 0;
            for (int i = 0; i < customerBase.Length; i++)
            {
                var digit = int.Parse(customerBase[i].ToString());
                sum += digit * (i + 1);
            }
            
            return sum % 10;
        }

        private void LogEvent(string message)
        {
            var logEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - CUSTOMER_SERVICE: {message}";
            System.Diagnostics.Trace.TraceInformation(logEntry);
        }
    }
}
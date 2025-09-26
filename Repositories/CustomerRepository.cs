using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmkcApi.Models;

namespace SmkcApi.Repositories
{
    /// <summary>
    /// In-memory implementation of Customer Repository for demonstration purposes.
    /// In production, this should be replaced with proper database implementation.
    /// </summary>
    public class CustomerRepository : ICustomerRepository
    {
        private static readonly ConcurrentDictionary<string, Customer> _customers = new ConcurrentDictionary<string, Customer>();
        
        static CustomerRepository()
        {
            SeedSampleData();
        }

        public async Task<Customer> GetCustomerAsync(string customerReference)
        {
            await Task.Delay(1); // Simulate async operation
            
            _customers.TryGetValue(customerReference, out Customer customer);
            return customer;
        }

        public async Task<Customer> CreateCustomerAsync(Customer customer)
        {
            await Task.Delay(1); // Simulate async operation
            
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            if (string.IsNullOrEmpty(customer.CustomerReference))
                throw new ArgumentException("Customer reference is required");

            if (_customers.ContainsKey(customer.CustomerReference))
                throw new InvalidOperationException($"Customer already exists: {customer.CustomerReference}");

            customer.CreatedDate = DateTime.UtcNow;
            customer.LastModifiedDate = DateTime.UtcNow;

            _customers.TryAdd(customer.CustomerReference, customer);
            return customer;
        }

        public async Task<Customer> UpdateCustomerAsync(Customer customer)
        {
            await Task.Delay(1); // Simulate async operation
            
            if (customer == null)
                return null;

            if (_customers.TryGetValue(customer.CustomerReference, out Customer existingCustomer))
            {
                customer.LastModifiedDate = DateTime.UtcNow;
                customer.CreatedDate = existingCustomer.CreatedDate; // Preserve creation date
                
                _customers.TryUpdate(customer.CustomerReference, customer, existingCustomer);
                return customer;
            }

            return null;
        }

        public async Task<bool> DeleteCustomerAsync(string customerReference)
        {
            await Task.Delay(1); // Simulate async operation
            
            return _customers.TryRemove(customerReference, out _);
        }

        public async Task<List<Customer>> GetCustomersByStatusAsync(string status)
        {
            await Task.Delay(1); // Simulate async operation
            
            return _customers.Values
                .Where(c => c.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .ToList();
        }

        public async Task<bool> UpdateKycStatusAsync(string customerReference, string kycStatus)
        {
            await Task.Delay(1); // Simulate async operation
            
            if (_customers.TryGetValue(customerReference, out Customer customer))
            {
                customer.KycStatus = kycStatus;
                customer.LastModifiedDate = DateTime.UtcNow;
                return true;
            }

            return false;
        }

        private static void SeedSampleData()
        {
            var sampleCustomers = new[]
            {
                new Customer
                {
                    CustomerReference = "CUST001",
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "john.doe@email.com",
                    PhoneNumber = "+1-555-0123",
                    DateOfBirth = new DateTime(1985, 6, 15),
                    NationalId = "123456789",
                    Address = new Address
                    {
                        Street = "123 Main Street",
                        City = "New York",
                        State = "NY",
                        PostalCode = "10001",
                        Country = "USA"
                    },
                    Status = "Active",
                    CreatedDate = DateTime.UtcNow.AddMonths(-8),
                    LastModifiedDate = DateTime.UtcNow.AddDays(-10),
                    KycStatus = "Verified"
                },
                new Customer
                {
                    CustomerReference = "CUST002",
                    FirstName = "Jane",
                    LastName = "Smith",
                    Email = "jane.smith@email.com",
                    PhoneNumber = "+1-555-0124",
                    DateOfBirth = new DateTime(1990, 3, 22),
                    NationalId = "987654321",
                    Address = new Address
                    {
                        Street = "456 Oak Avenue",
                        City = "Los Angeles",
                        State = "CA",
                        PostalCode = "90001",
                        Country = "USA"
                    },
                    Status = "Active",
                    CreatedDate = DateTime.UtcNow.AddYears(-1),
                    LastModifiedDate = DateTime.UtcNow.AddDays(-3),
                    KycStatus = "Verified"
                },
                new Customer
                {
                    CustomerReference = "CUST003",
                    FirstName = "Robert",
                    LastName = "Johnson",
                    Email = "robert.johnson@email.com",
                    PhoneNumber = "+1-555-0125",
                    DateOfBirth = new DateTime(1978, 11, 8),
                    NationalId = "456789123",
                    Address = new Address
                    {
                        Street = "789 Pine Road",
                        City = "Chicago",
                        State = "IL",
                        PostalCode = "60601",
                        Country = "USA"
                    },
                    Status = "Inactive",
                    CreatedDate = DateTime.UtcNow.AddMonths(-2),
                    LastModifiedDate = DateTime.UtcNow.AddDays(-1),
                    KycStatus = "Pending"
                }
            };

            foreach (var customer in sampleCustomers)
            {
                _customers.TryAdd(customer.CustomerReference, customer);
            }
        }

        /// <summary>
        /// Get all customers for administrative purposes (not exposed via API)
        /// </summary>
        public List<Customer> GetAllCustomers()
        {
            return _customers.Values.ToList();
        }

        /// <summary>
        /// Clear all data (for testing purposes only)
        /// </summary>
        public void ClearAllData()
        {
            _customers.Clear();
            SeedSampleData();
        }
    }
}
using System;

namespace SmkcApi.Models
{
    public class Customer
    {
        public string CustomerReference { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string NationalId { get; set; }
        public Address Address { get; set; }
        public string Status { get; set; } // Active, Inactive, Suspended
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public string KycStatus { get; set; } // Pending, Verified, Rejected
    }

    public class Address
    {
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
    }

    public class CustomerRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string NationalId { get; set; }
        public Address Address { get; set; }
    }

    public class CustomerUpdateRequest
    {
        public string CustomerReference { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public Address Address { get; set; }
    }
}
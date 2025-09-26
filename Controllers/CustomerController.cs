using System;
using System.Threading.Tasks;
using System.Web.Http;
using System.Net; // added
using SmkcApi.Models;
using SmkcApi.Security;
using SmkcApi.Services;

namespace SmkcApi.Controllers
{
    [RoutePrefix("api/customers")]
    [ShaAuthentication]
    [IPWhitelist]
    [RateLimit(maxRequests: 50, timeWindowMinutes: 1)]
    public class CustomerController : ApiController
    {
        private readonly ICustomerService _customerService;

        public CustomerController(ICustomerService customerService)
        {
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
        }

        /// <summary>
        /// Get customer details by customer reference
        /// </summary>
        /// <param name="customerReference">The customer reference</param>
        /// <returns>Customer details</returns>
        [HttpGet]
        [Route("{customerReference}")]
        public async Task<IHttpActionResult> GetCustomer(string customerReference)
        {
            try
            {
                if (string.IsNullOrEmpty(customerReference))
                {
                    return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError("Customer reference is required", "INVALID_CUSTOMER_REFERENCE"));
                }

                var customer = await _customerService.GetCustomerAsync(customerReference);
                
                if (customer == null)
                {
                    return NotFound();
                }

                return Ok(ApiResponse<Customer>.CreateSuccess(customer, "Customer retrieved successfully"));
            }
            catch (ArgumentException ex)
            {
                return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError(ex.Message, "VALIDATION_ERROR"));
            }
            catch (Exception ex)
            {
                LogError("GetCustomer", ex);
                return InternalServerError();
            }
        }

        /// <summary>
        /// Create a new customer
        /// </summary>
        /// <param name="request">Customer creation request</param>
        /// <returns>Created customer details</returns>
        [HttpPost]
        [Route("")]
        public async Task<IHttpActionResult> CreateCustomer([FromBody] CustomerRequest request)
        {
            try
            {
                if (request == null)
                {
                    return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError("Request body is required", "MISSING_REQUEST_BODY"));
                }

                var customer = await _customerService.CreateCustomerAsync(request);
                
                return Ok(ApiResponse<Customer>.CreateSuccess(customer, "Customer created successfully"));
            }
            catch (ArgumentException ex)
            {
                return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError(ex.Message, "VALIDATION_ERROR"));
            }
            catch (Exception ex)
            {
                LogError("CreateCustomer", ex);
                return InternalServerError();
            }
        }

        /// <summary>
        /// Update customer information
        /// </summary>
        /// <param name="request">Customer update request</param>
        /// <returns>Updated customer details</returns>
        [HttpPut]
        [Route("")]
        public async Task<IHttpActionResult> UpdateCustomer([FromBody] CustomerUpdateRequest request)
        {
            try
            {
                if (request == null)
                {
                    return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError("Request body is required", "MISSING_REQUEST_BODY"));
                }

                var customer = await _customerService.UpdateCustomerAsync(request);
                
                if (customer == null)
                {
                    return NotFound();
                }

                return Ok(ApiResponse<Customer>.CreateSuccess(customer, "Customer updated successfully"));
            }
            catch (ArgumentException ex)
            {
                return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError(ex.Message, "VALIDATION_ERROR"));
            }
            catch (Exception ex)
            {
                LogError("UpdateCustomer", ex);
                return InternalServerError();
            }
        }

        /// <summary>
        /// Validate customer exists
        /// </summary>
        /// <param name="customerReference">The customer reference</param>
        /// <returns>Validation result</returns>
        [HttpGet]
        [Route("{customerReference}/validate")]
        public async Task<IHttpActionResult> ValidateCustomer(string customerReference)
        {
            try
            {
                if (string.IsNullOrEmpty(customerReference))
                {
                    return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError("Customer reference is required", "INVALID_CUSTOMER_REFERENCE"));
                }

                var isValid = await _customerService.ValidateCustomerAsync(customerReference);
                
                var result = new { customerReference, isValid, status = isValid ? "Valid" : "Invalid" };
                return Ok(ApiResponse<object>.CreateSuccess(result, "Customer validation completed"));
            }
            catch (Exception ex)
            {
                LogError("ValidateCustomer", ex);
                return InternalServerError();
            }
        }

        /// <summary>
        /// Update customer KYC status
        /// </summary>
        /// <param name="customerReference">The customer reference</param>
        /// <param name="request">KYC status update request</param>
        /// <returns>Update result</returns>
        [HttpPut]
        [Route("{customerReference}/kyc")]
        public async Task<IHttpActionResult> UpdateKycStatus(string customerReference, [FromBody] KycStatusUpdateRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(customerReference))
                {
                    return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError("Customer reference is required", "INVALID_CUSTOMER_REFERENCE"));
                }

                if (request == null || string.IsNullOrEmpty(request.KycStatus))
                {
                    return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError("KYC status is required", "MISSING_KYC_STATUS"));
                }

                var result = await _customerService.UpdateKycStatusAsync(customerReference, request.KycStatus);
                
                if (!result)
                {
                    return NotFound();
                }

                return Ok(ApiResponse<object>.CreateSuccess(new { customerReference, kycStatus = request.KycStatus }, "KYC status updated successfully"));
            }
            catch (ArgumentException ex)
            {
                return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError(ex.Message, "VALIDATION_ERROR"));
            }
            catch (Exception ex)
            {
                LogError("UpdateKycStatus", ex);
                return InternalServerError();
            }
        }

        private void LogError(string action, Exception ex)
        {
            var logEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - CUSTOMER_CONTROLLER_{action.ToUpper()}_ERROR: {ex.Message}";
            System.Diagnostics.Trace.TraceError(logEntry);
            
            // Additional security logging with request context
            if (Request.Properties.ContainsKey("ApiKey"))
            {
                var apiKey = Request.Properties["ApiKey"].ToString();
                var maskedApiKey = SecurityHelper.MaskSensitiveData(apiKey);
                var requestId = Request.Properties.ContainsKey("RequestId") ? Request.Properties["RequestId"].ToString() : "Unknown";
                
                var securityLogEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - SECURITY_ERROR - Action: {action}, " +
                                      $"ApiKey: {maskedApiKey}, RequestId: {requestId}, Error: {ex.Message}";
                System.Diagnostics.Trace.TraceError(securityLogEntry);
            }
        }
    }
}
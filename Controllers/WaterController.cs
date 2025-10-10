using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using SmkcApi.Models;
using SmkcApi.Security;
using SmkcApi.Services;

namespace SmkcApi.Controllers
{
    [RoutePrefix("api/water")]
    [ShaAuthentication]
    [IPWhitelist]
    [RateLimit(maxRequests: 120, timeWindowMinutes: 1)]
    public class WaterController : ApiController
    {
        private readonly IWaterService _svc;
        public WaterController(IWaterService svc) { _svc = svc; }

        [HttpPost, Route("bill/fetch")]
        public async Task<IHttpActionResult> BillFetch([FromBody] BillFetchRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.ConsumerNo))
                return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError("consumerNo is required", "INVALID_CONSUMER_NO"));

            var result = await _svc.FetchBillAsync(req);
            if (result == null) return NotFound();

            return Ok(ApiResponse<BillFetchResponse>.CreateSuccess(result, "Bill fetched"));
        }

        [HttpPost, Route("collection")]
        public async Task<IHttpActionResult> PostCollection([FromBody] CollectionPostRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.ConsumerNo) || req.Amount <= 0 || string.IsNullOrWhiteSpace(req.BankTxnId))
                return Content(HttpStatusCode.BadRequest, ApiResponse<object>.CreateError("Invalid request", "VALIDATION_ERROR"));

            var idem = Request.Headers.Contains("X-Idempotency-Key")
                ? string.Join("", Request.Headers.GetValues("X-Idempotency-Key"))
                : null;

            var result = await _svc.PostCollectionAsync(req, idem);
            switch (result.Status)
            {
                case CollectionPostStatus.Success:
                    return Ok(ApiResponse<CollectionPostResponse>.CreateSuccess(result.Payload, "Collection posted"));
                case CollectionPostStatus.Duplicate:
                    return Content(HttpStatusCode.Conflict, ApiResponse<CollectionPostResponse>.CreateSuccess(result.Payload, "Duplicate - returning existing receipt"));
                case CollectionPostStatus.NotFound:
                    return NotFound();
                case CollectionPostStatus.BusinessRuleFailed:
                    return Content((HttpStatusCode)422, ApiResponse<object>.CreateError(result.ErrorMessage, "BUSINESS_RULE"));
                default:
                    return InternalServerError();
            }
        }
    }
}

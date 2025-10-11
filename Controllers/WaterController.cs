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

        // Bill Fetch: POST /api/water/bill/fetch
        [HttpPost, Route("bill/fetch")]
        public async Task<IHttpActionResult> BillFetch([FromBody] BillFetchRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.ConsumerNo))
                return Content(HttpStatusCode.BadRequest,
                    ApiResponse<object>.CreateError("consumerNo is required", "INVALID_CONSUMER_NO"));

            var result = await _svc.FetchBillAsync(req);

            if (result == null)
                return NotFound();

            return Ok(ApiResponse<BillFetchResponse>.CreateSuccess(result, "Bill fetched"));
        }
    }
}

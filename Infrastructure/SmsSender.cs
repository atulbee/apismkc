using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Configuration;

namespace SmkcApi.Infrastructure
{
    public interface ISmsSender
    {
        Task<string> SendSmsRawAsync(IEnumerable<string> numbers, string message, bool unicode = true);
    }

    public class SmsSender : ISmsSender, IDisposable
    {
        private readonly HttpClient _http;
        private readonly string _user;
        private readonly string _password;
        private readonly string _senderId;
        private readonly string _channel;
        private readonly string _route;
        private readonly string _peid;
        private readonly string _dcsUnicode;

        public SmsSender()
        {
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _user = ConfigurationManager.AppSettings["Sms_User"];
            _password = ConfigurationManager.AppSettings["Sms_Password"];
            _senderId = ConfigurationManager.AppSettings["Sms_SenderId"];
            _channel = ConfigurationManager.AppSettings["Sms_Channel"] ?? "Promo";
            _route = ConfigurationManager.AppSettings["Sms_DefaultRoute"] ?? "";
            _peid = ConfigurationManager.AppSettings["Sms_DefaultPeId"] ?? "";
            _dcsUnicode = ConfigurationManager.AppSettings["Sms_DCS_Unicode"] ?? "8";
        }

        public async Task<string> SendSmsRawAsync(IEnumerable<string> numbers, string message, bool unicode = true)
        {
            // provider base URL (from your doc)
            var baseUrl = "http://sms.auurumdigital.com/api/mt/SendSMS";

            // join numbers by comma; provider allows up to 100 numbers per request (per page html)
            var numberCsv = string.Join(",", numbers);

            // URL encode message
            var encodedMessage = HttpUtility.UrlEncode(message);

            var uriBuilder = new UriBuilder(baseUrl);
            var qs = HttpUtility.ParseQueryString(string.Empty);
            qs["user"] = _user;
            qs["password"] = _password;
            qs["senderid"] = _senderId;
            qs["channel"] = _channel;
            qs["DCS"] = unicode ? _dcsUnicode : "0";
            qs["flashsms"] = "0";
            qs["number"] = numberCsv;
            qs["text"] = message; // will be encoded by query builder below
            if (!string.IsNullOrWhiteSpace(_route)) qs["route"] = _route;
            if (!string.IsNullOrWhiteSpace(_peid)) qs["peid"] = _peid;

            uriBuilder.Query = qs.ToString();

            var requestUri = uriBuilder.Uri; // Note: HttpUtility doesn't auto-encode text in QueryString; but UriBuilder/Query does

            // Some providers prefer GET; if provider supports POST JSON, switch to POST.
            var response = await _http.GetAsync(requestUri);
            string body = await response.Content.ReadAsStringAsync();
            // You should parse body (JSON) for structured success; for now return raw
            return body;
        }

        public void Dispose() => _http?.Dispose();
    }
}

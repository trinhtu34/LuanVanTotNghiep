using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using testpayment6._0.Areas.admin.Models;

namespace testpayment6._0.Areas.admin.Controllers
{
    [Area("admin")]
    public class ShowOrdertablePaymentHistoryController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string BASE_API_URL;

        public ShowOrdertablePaymentHistoryController(HttpClient httpClient , IConfiguration configuration)
        {
            _httpClient = httpClient;
            BASE_API_URL = configuration["BaseAPI"];
        }

        public async Task<IActionResult> Index(bool? filterBySuccess = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var viewModel = new PaymentHistoryViewModel_OrderTable
            {
                FilterBySuccess = filterBySuccess,
                FromDate = fromDate,
                ToDate = toDate
            };

            try
            {
                var paymentHistory = await GetPaymentHistoryWithFiltersAsync(fromDate, toDate, filterBySuccess);
                viewModel.PaymentHistory = paymentHistory;
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Có lỗi xảy ra: {ex.Message}";
            }

            return View(viewModel);
        }

        public async Task<List<PaymentHistoryModel_OrderTable>> GetPaymentHistoryWithFiltersAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            bool? filterBySuccess = null)
        {
            try
            {
                var queryParameters = new List<string>();
                
                if (fromDate.HasValue)
                    queryParameters.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
                if(toDate.HasValue)
                    queryParameters.Add($"toDate={toDate.Value:yyyy-MM-dd}");
                if (filterBySuccess.HasValue)
                    queryParameters.Add($"filterBySuccess={filterBySuccess.Value}");
                var queryString = queryParameters.Count > 0 ? "?" + string.Join("&", queryParameters) : "";
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/payment/paymenthistory/ordertable/filter{queryString}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<List<PaymentHistoryModel_OrderTable>>(content) ?? new List<PaymentHistoryModel_OrderTable>();
                }
                else
                {
                    throw new Exception($"Error fetching payment history: {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching payment history: {ex.Message}");
            }
        }
    }
}
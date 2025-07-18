using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using testpayment6._0.Areas.admin.Models;

namespace testpayment6._0.Areas.admin.Controllers
{
    [Area("admin")]
    public class ShowCartPaymentHistoryController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string BASE_API_URL;

        public ShowCartPaymentHistoryController(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            BASE_API_URL = configuration["BaseAPI"];
        }

        public async Task<IActionResult> Index(bool? filterBySuccess = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var viewModel = new PaymentHistoryViewModel_Cart
            {
                FilterBySuccess = filterBySuccess,
                FromDate = fromDate,
                ToDate = toDate
            };

            try
            {
                // Luôn gọi API filter với các tham số (có thể null)
                var paymentHistory = await GetPaymentHistoryWithFiltersAsync(fromDate, toDate, filterBySuccess);
                viewModel.PaymentHistory = paymentHistory;
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Có lỗi xảy ra: {ex.Message}";
            }

            return View(viewModel);
        }

        // API duy nhất xử lý tất cả filter
        public async Task<List<PaymentHistoryModel_Cart>> GetPaymentHistoryWithFiltersAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            bool? filterBySuccess = null)
        {
            try
            {
                var queryParams = new List<string>();

                if (fromDate.HasValue)
                    queryParams.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");

                if (toDate.HasValue)
                    queryParams.Add($"toDate={toDate.Value:yyyy-MM-dd}");

                if (filterBySuccess.HasValue)
                    queryParams.Add($"filterBySuccess={filterBySuccess.Value}");

                var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";

                var response = await _httpClient.GetAsync($"{BASE_API_URL}/payment/paymenthistory/cart/filter{queryString}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<List<PaymentHistoryModel_Cart>>(jsonContent) ?? new List<PaymentHistoryModel_Cart>();
                }
                else
                {
                    throw new Exception("Không thể tải dữ liệu lịch sử thanh toán");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Có lỗi xảy ra: {ex.Message}");
            }
        }
    }
}
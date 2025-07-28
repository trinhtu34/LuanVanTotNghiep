using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using testpayment6._0.Areas.admin.Models;

namespace testpayment6._0.Areas.admin.Controllers
{
    [Area("admin")]
    public class ShowAllPaymentHistoryController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AddFoodOnOrderTableController> _logger;
        private readonly string BASE_API_URL;

        public ShowAllPaymentHistoryController(HttpClient httpClient, IConfiguration configuration, ILogger<AddFoodOnOrderTableController> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            BASE_API_URL = configuration["BaseAPI"];
        }

        [HttpGet]
        public async Task<IActionResult> Index(bool? filterBySuccess = null , DateTime? fromDate = null , DateTime? toDate= null)
        {
            var viewModel = new PaymentHistoryViewAll
            {
                FilterBySuccess = filterBySuccess,
                FromDate = fromDate,
                ToDate = toDate
            };
            try
            {
                var paymentHistory = await GetPaymentHistory(fromDate, toDate, filterBySuccess);
                viewModel.PaymentHistory = paymentHistory;
                viewModel.TotalAmount = paymentHistory
                    .Where(p => p.IsSuccess == true)
                    .Sum(p => p.Amount);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Có lỗi xảy ra: {ex.Message}";
            }
            return View(viewModel);
        }
        public async Task<List<totalPaymentInfo>> GetPaymentHistory(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            bool? filterBySuccess = null
        )
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
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/Payment{queryString}");
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<List<totalPaymentInfo>>(jsonContent) ?? new List<totalPaymentInfo>();
                }
                else
                {
                    throw new Exception("Không thể tải dữ liệu lịch sử thanh toán");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching payment history");
                throw new Exception($"Có lỗi xảy ra: {ex.Message}");
            }
        }
    }
}

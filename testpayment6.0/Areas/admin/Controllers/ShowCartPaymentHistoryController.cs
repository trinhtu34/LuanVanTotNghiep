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

        public ShowCartPaymentHistoryController(HttpClient httpClient , IConfiguration configuration)
        {
            _httpClient = httpClient;
            BASE_API_URL = configuration["BaseAPI"];
        }

        public async Task<IActionResult> Index(bool? filterBySuccess = null)
        {
            var viewModel = new PaymentHistoryViewModel_Cart
            {
                FilterBySuccess = filterBySuccess
            };
            try
            {
                var paymentHistory = await GetPaymentHistoryAsync();
                // Áp dụng filter nếu có
                if (filterBySuccess.HasValue)
                {
                    paymentHistory = paymentHistory.Where(p => p.IsSuccess == filterBySuccess.Value).ToList();
                }
                viewModel.PaymentHistory = paymentHistory;
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Có lỗi xảy ra: {ex.Message}";
            }
            return View(viewModel);
        }

        public async Task<List<PaymentHistoryModel_Cart>> GetPaymentHistoryAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/payment/paymenthistory/cart");
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
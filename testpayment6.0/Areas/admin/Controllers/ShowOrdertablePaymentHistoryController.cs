using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using testpayment6._0.Areas.admin.Models;

namespace testpayment6._0.Areas.admin.Controllers
{
    [Area("admin")]
    public class ShowOrdertablePaymentHistoryController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl = "https://p7igzosmei.execute-api.ap-southeast-1.amazonaws.com/Prod/api/payment/paymenthistory/ordertable";

        public ShowOrdertablePaymentHistoryController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IActionResult> Index(bool? filterBySuccess = null)
        {
            var viewModel = new PaymentHistoryViewModel_OrderTable
            {
                FilterBySuccess = filterBySuccess
            };

            try
            {
                var response = await _httpClient.GetAsync(_apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var paymentHistory = JsonConvert.DeserializeObject<List<PaymentHistoryModel_OrderTable>>(jsonContent);

                    // filter , nếu không dùng - clear thì in ra tất cả 
                    if (filterBySuccess.HasValue)
                    {
                        paymentHistory = paymentHistory.Where(p => p.IsSuccess == filterBySuccess.Value).ToList();
                    }

                    viewModel.PaymentHistory = paymentHistory ?? new List<PaymentHistoryModel_OrderTable>();
                }
                else
                {
                    ViewBag.ErrorMessage = "Không thể tải dữ liệu lịch sử thanh toán";
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Có lỗi xảy ra: {ex.Message}";
            }

            return View(viewModel);
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using testpayment6._0.Areas.admin.Models;

namespace testpayment6._0.Areas.admin.Controllers
{
    [Area("admin")]
    public class OrdertablePaymentController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string BASE_API_URL;

        public OrdertablePaymentController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            BASE_API_URL = configuration["BaseAPI"];
        }

        [HttpGet]
        public IActionResult Index()
        {
            // Kiểm tra đăng nhập
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("IndexAdminLogin", "HomeAdmin");
            }

            ViewBag.UserId = userId;
            return View(new OrdertablePaymentViewModel_adminPayment());
        }

        // POST: Payment/Index
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(OrdertablePaymentViewModel_adminPayment model)
        {
            // Kiểm tra đăng nhập
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("IndexAdminLogin", "HomeAdmin");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.UserId = userId;
                return View(model);
            }

            try
            {
                using var client = _httpClientFactory.CreateClient();

                var paymentData = new
                {
                    Amount = model.Amount
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(paymentData),
                    Encoding.UTF8,
                    "application/json"
                );

                var apiUrl = $"{BASE_API_URL}/payment/admin/ordertable/{model.OrdertableId}";
                var response = await client.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    ViewBag.Success = true;
                    ViewBag.Message = "Thanh toán đã được lưu thành công!";
                    ViewBag.OrdertableId = model.OrdertableId;
                    ViewBag.Amount = model.Amount;

                    // Reset form
                    ModelState.Clear();
                    model = new OrdertablePaymentViewModel_adminPayment();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ViewBag.Success = false;
                    ViewBag.Message = $"Lỗi khi lưu thanh toán: {response.StatusCode}";
                    Console.WriteLine($"Payment API Error: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Success = false;
                ViewBag.Message = $"Lỗi hệ thống: {ex.Message}";
                Console.WriteLine($"Payment Exception: {ex.Message}");
            }

            ViewBag.UserId = userId;
            return View(model);
        }

        // API endpoint để kiểm tra session
        [HttpGet]
        public IActionResult CheckSession()
        {
            var userId = HttpContext.Session.GetString("UserId");
            return Json(new { isAuthenticated = !string.IsNullOrEmpty(userId) });
        }
    }
}
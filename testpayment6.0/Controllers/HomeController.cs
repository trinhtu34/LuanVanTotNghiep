// Controllers/HomeController.cs - Tích hợp Login
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using testpayment6._0.Models;
using testpayment6._0.ResponseModels;

namespace testpayment6._0.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly HttpClient _httpClient;
        private readonly string BASE_API_URL;

        public HomeController(ILogger<HomeController> logger, HttpClient httpClient , IConfiguration configuration)
        {
            _logger = logger;
            _httpClient = httpClient;
            BASE_API_URL = configuration["BaseAPI"];
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // === LOGIN FUNCTIONS ===
        [HttpGet]
        public IActionResult Login()
        {
            // Nếu đã đăng nhập thì về trang chủ
            if (HttpContext.Session.GetString("UserId") != null)
                return RedirectToAction("Index");

            return View();
        }

        [Route("Home/Login")]
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {

                // Gọi API đăng nhập
                var request = new { UserId = model.UserId, UPassword = model.UPassword };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{BASE_API_URL}/user/login",content);

                if (response.IsSuccessStatusCode)
                {
                    // Lưu vào Session
                    HttpContext.Session.SetString("UserId", model.UserId);
                    TempData["Message"] = "Đăng nhập thành công!";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error");
            }

            ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng";
            return View(model);
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["Message"] = "Đăng xuất thành công!";
            return RedirectToAction("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
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

        public HomeController(ILogger<HomeController> logger, HttpClient httpClient, IConfiguration configuration)
        {
            _logger = logger;
            _httpClient = httpClient;
            BASE_API_URL = configuration["BaseAPI"];
        }

        public IActionResult Index()
        {
            // Kiểm tra trạng thái đăng nhập
            ViewBag.IsLoggedIn = !string.IsNullOrEmpty(HttpContext.Session.GetString("UserId"));
            ViewBag.UserId = HttpContext.Session.GetString("UserId");
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            // Nếu đã đăng nhập thì về trang chủ
            if (IsUserLoggedIn())
            {
                _logger.LogInformation($"User {HttpContext.Session.GetString("UserId")} already logged in, redirecting to Index");
                return RedirectToAction("Index");
            }
            return View();
        }

        [Route("Home/Login")]
        [HttpPost]
        [ValidateAntiForgeryToken] // Thêm bảo mật CSRF
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

                var response = await _httpClient.PostAsync($"{BASE_API_URL}/user/login", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();

                    // Lưu vào Session với nhiều thông tin hơn
                    HttpContext.Session.SetString("UserId", model.UserId);
                    HttpContext.Session.SetString("IsAuthenticated", "true");
                    HttpContext.Session.SetString("LoginTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    // Commit session ngay lập tức
                    await HttpContext.Session.CommitAsync();

                    TempData["Message"] = "Đăng nhập thành công!";
                    return RedirectToAction("Index");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng";
                }
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, $"Network error during login for user {model.UserId}");
                ViewBag.Error = "Không thể kết nối đến server. Vui lòng thử lại.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error during login for user {model.UserId}");
                ViewBag.Error = "Đã xảy ra lỗi. Vui lòng thử lại.";
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Thêm bảo mật CSRF
        public async Task<IActionResult> Logout()
        {
            var userId = HttpContext.Session.GetString("UserId");

            _logger.LogInformation($"User {userId} logging out. Session ID: {HttpContext.Session.Id}");

            // Clear session
            HttpContext.Session.Clear();

            // Commit session changes
            await HttpContext.Session.CommitAsync();

            TempData["Message"] = "Đăng xuất thành công!";
            return RedirectToAction("Index");
        }

        // Helper method để kiểm tra trạng thái đăng nhập
        private bool IsUserLoggedIn()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var isAuthenticated = HttpContext.Session.GetString("IsAuthenticated");

            return !string.IsNullOrEmpty(userId) && isAuthenticated == "true";
        }

        // Action để kiểm tra trạng thái session (dùng cho debugging)
        [HttpGet]
        public IActionResult CheckSession()
        {
            var sessionInfo = new
            {
                SessionId = HttpContext.Session.Id,
                UserId = HttpContext.Session.GetString("UserId"),
                IsAuthenticated = HttpContext.Session.GetString("IsAuthenticated"),
                LoginTime = HttpContext.Session.GetString("LoginTime"),
                AllKeys = HttpContext.Session.Keys.ToList()
            };

            return Json(sessionInfo);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
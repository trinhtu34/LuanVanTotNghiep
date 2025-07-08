using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using testpayment6._0.Models;
using testpayment6._0.ResponseModels;
using System.Linq;

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
        public async Task<IActionResult> Index()
        {
            // Kiểm tra trạng thái đăng nhập
            ViewBag.IsLoggedIn = !string.IsNullOrEmpty(HttpContext.Session.GetString("UserId"));
            ViewBag.UserId = HttpContext.Session.GetString("UserId");

            // Gọi API để lấy số lượng món ăn
            try
            {
                using (var httpClient = new HttpClient())
                {
                    // Gọi API count
                    var countResponse = await httpClient.GetAsync("https://p7igzosmei.execute-api.ap-southeast-1.amazonaws.com/Prod/api/menu/quantity/count");
                    if (countResponse.IsSuccessStatusCode)
                    {
                        var countContent = await countResponse.Content.ReadAsStringAsync();
                        if (int.TryParse(countContent, out int menuCount))
                        {
                            ViewBag.MenuCount = menuCount;
                        }
                        else
                        {
                            ViewBag.MenuCount = 50; // Fallback value
                        }
                    }
                    else
                    {
                        ViewBag.MenuCount = 50; // Fallback value
                    }

                    // Gọi API featured menu
                    var menuResponse = await httpClient.GetAsync("https://p7igzosmei.execute-api.ap-southeast-1.amazonaws.com/Prod/api/menu/quantity");
                    if (menuResponse.IsSuccessStatusCode)
                    {
                        var menuContent = await menuResponse.Content.ReadAsStringAsync();
                        var menuItems = JsonSerializer.Deserialize<List<dynamic>>(menuContent, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        // Sắp xếp theo count giảm dần, lấy 3-6 món
                        var featuredMenu = menuItems
                            .OrderByDescending(x => ((JsonElement)x).GetProperty("count").GetInt32())
                            .Take(3)
                            .ToList();

                        ViewBag.FeaturedMenu = featuredMenu;
                    }
                }
            }
            catch
            {
                ViewBag.MenuCount = 50; // Fallback value nếu có lỗi
                ViewBag.FeaturedMenu = new List<dynamic>(); // Empty list
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        public IActionResult MyOrders()
        {
            // Kiểm tra đăng nhập
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("login");
            }

            // Chuyển hướng đến trang đơn hàng
            return RedirectToAction("Index", "Order");
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
        // Thêm các method này vào HomeController.cs của bạn

        [HttpGet]
        public IActionResult Register()
        {
            // Nếu đã đăng nhập thì về trang chủ
            if (IsUserLoggedIn())
            {
                _logger.LogInformation($"User {HttpContext.Session.GetString("UserId")} already logged in, redirecting to Index");
                return RedirectToAction("Index");
            }
            return View();
        }

        [Route("Home/Register")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                // Gọi API đăng ký
                var request = new
                {
                    UserId = model.UserId,
                    UPassword = model.UPassword,
                    CustomerName = model.CustomerName,
                    PhoneNumber = model.PhoneNumber,
                    Email = model.Email ?? "",
                    Address = model.Address ?? ""
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation($"Calling register API for user: {model.UserId}");
                var response = await _httpClient.PostAsync($"{BASE_API_URL}/user/signup", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"User {model.UserId} registered successfully");

                    TempData["Message"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                    return RedirectToAction("Login");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Register failed for user {model.UserId}: {response.StatusCode} - {errorContent}");

                    // Xử lý các lỗi cụ thể từ API
                    if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                    {
                        ViewBag.Error = "Tên đăng nhập đã tồn tại. Vui lòng chọn tên khác.";
                    }
                    else
                    {
                        ViewBag.Error = "Đăng ký không thành công. Vui lòng thử lại.";
                    }
                }
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, $"Network error during registration for user {model.UserId}");
                ViewBag.Error = "Không thể kết nối đến server. Vui lòng thử lại.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error during registration for user {model.UserId}");
                ViewBag.Error = "Đã xảy ra lỗi. Vui lòng thử lại.";
            }

            return View(model);
        }
        // Thêm các method này vào HomeController.cs của bạn

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            // Kiểm tra đăng nhập
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login");
            }

            var userId = HttpContext.Session.GetString("UserId");

            try
            {
                // Gọi API để lấy thông tin profile hiện tại
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/user/{userId}");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var userProfile = JsonSerializer.Deserialize<UserProfileResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    var model = new ProfileViewModel
                    {
                        UserId = userId,
                        CustomerName = userProfile?.CustomerName ?? "",
                        PhoneNumber = userProfile?.PhoneNumber ?? "",
                        Email = userProfile?.Email ?? "",
                        Address = userProfile?.Address ?? ""
                    };

                    return View(model);
                }
                else
                {
                    _logger.LogWarning($"Failed to get profile for user {userId}: {response.StatusCode}");
                    // Nếu không lấy được thông tin, tạo model rỗng
                    var model = new ProfileViewModel
                    {
                        UserId = userId,
                        CustomerName = "",
                        PhoneNumber = "",
                        Email = "",
                        Address = ""
                    };
                    ViewBag.Error = "Không thể tải thông tin profile. Vui lòng thử lại.";
                    return View(model);
                }
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, $"Network error getting profile for user {userId}");
                ViewBag.Error = "Không thể kết nối đến server. Vui lòng thử lại.";

                var model = new ProfileViewModel
                {
                    UserId = userId,
                    CustomerName = "",
                    PhoneNumber = "",
                    Email = "",
                    Address = ""
                };
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error getting profile for user {userId}");
                ViewBag.Error = "Đã xảy ra lỗi. Vui lòng thử lại.";

                var model = new ProfileViewModel
                {
                    UserId = userId,
                    CustomerName = "",
                    PhoneNumber = "",
                    Email = "",
                    Address = ""
                };
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            // Kiểm tra đăng nhập
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login");
            }

            var userId = HttpContext.Session.GetString("UserId");
            model.UserId = userId; // Đảm bảo UserId không bị thay đổi

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Chuẩn bị dữ liệu để gửi API
                var request = new
                {
                    UPassword = model.UPassword,
                    CustomerName = string.IsNullOrWhiteSpace(model.CustomerName) ? null : model.CustomerName.Trim(),
                    PhoneNumber = model.PhoneNumber,
                    Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim(),
                    Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim()
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation($"Updating profile for user: {userId}");
                var response = await _httpClient.PutAsync($"{BASE_API_URL}/user/modify/{userId}", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Profile updated successfully for user {userId}");

                    TempData["Message"] = "Cập nhật thông tin thành công!";
                    return RedirectToAction("Profile");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Profile update failed for user {userId}: {response.StatusCode} - {errorContent}");

                    ViewBag.Error = "Cập nhật thông tin không thành công. Vui lòng thử lại.";
                }
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, $"Network error during profile update for user {userId}");
                ViewBag.Error = "Không thể kết nối đến server. Vui lòng thử lại.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error during profile update for user {userId}");
                ViewBag.Error = "Đã xảy ra lỗi. Vui lòng thử lại.";
            }

            return View(model);
        }
        // Thêm các methods này vào HomeController.cs

        [HttpGet]
        public IActionResult ChangePassword()
        {
            // Kiểm tra đăng nhập
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login");
            }

            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            // Kiểm tra đăng nhập
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = HttpContext.Session.GetString("UserId");

            try
            {
                // Bước 1: Xác thực mật khẩu hiện tại bằng cách gọi API login
                var loginRequest = new { UserId = userId, UPassword = model.CurrentPassword };
                var loginJson = JsonSerializer.Serialize(loginRequest);
                var loginContent = new StringContent(loginJson, Encoding.UTF8, "application/json");

                var loginResponse = await _httpClient.PostAsync($"{BASE_API_URL}/user/login", loginContent);

                if (!loginResponse.IsSuccessStatusCode)
                {
                    ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng");
                    return View(model);
                }

                // Bước 2: Lấy thông tin user hiện tại
                var userResponse = await _httpClient.GetAsync($"{BASE_API_URL}/user/{userId}");
                if (!userResponse.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Không thể lấy thông tin người dùng. Vui lòng thử lại.";
                    return View(model);
                }

                var userContent = await userResponse.Content.ReadAsStringAsync();
                var userProfile = JsonSerializer.Deserialize<UserProfileResponse>(userContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // Bước 3: Cập nhật mật khẩu mới
                var updateRequest = new
                {
                    UPassword = model.NewPassword,
                    CustomerName = userProfile?.CustomerName ?? "",
                    PhoneNumber = userProfile?.PhoneNumber ?? "",
                    Email = userProfile?.Email ?? "",
                    Address = userProfile?.Address ?? ""
                };

                var updateJson = JsonSerializer.Serialize(updateRequest);
                var updateContent = new StringContent(updateJson, Encoding.UTF8, "application/json");

                var updateResponse = await _httpClient.PutAsync($"{BASE_API_URL}/user/modify/{userId}", updateContent);

                if (updateResponse.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Password changed successfully for user {userId}");
                    TempData["Message"] = "Thay đổi mật khẩu thành công!";
                    return RedirectToAction("Profile");
                }
                else
                {
                    var errorContent = await updateResponse.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Password change failed for user {userId}: {updateResponse.StatusCode} - {errorContent}");
                    ViewBag.Error = "Thay đổi mật khẩu không thành công. Vui lòng thử lại.";
                }
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, $"Network error during password change for user {userId}");
                ViewBag.Error = "Không thể kết nối đến server. Vui lòng thử lại.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error during password change for user {userId}");
                ViewBag.Error = "Đã xảy ra lỗi. Vui lòng thử lại.";
            }

            return View(model);
        }
    }
}
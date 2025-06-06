using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using testpayment6._0.ResponseModels;
using testpayment6._0.Attributes; // Import namespace của attribute

namespace testpayment6._0.Areas.admin.Controllers
{
    [Area("admin")]
    public class HomeAdminController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public HomeAdminController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                // Thêm cache-busting parameter
                //return Redirect($"/admin/homeadmin/indexadminlogin?t={DateTime.Now.Ticks}");

                return RedirectToAction("IndexAdminLogin");
            }

            ViewBag.UserId = userId;
            return View();
        }


        public IActionResult IndexAdminLogin()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> IndexAdminLogin(string userId, string uPassword)
        {
            using var client = _httpClientFactory.CreateClient();
            var content = new StringContent(
                JsonConvert.SerializeObject(new { UserId = userId, UPassword = uPassword }),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync("https://9ennjx1tb5.execute-api.ap-southeast-1.amazonaws.com/Prod/api/user/login", content);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();

                // Giải mã JSON response thành object
                var loginResult = JsonConvert.DeserializeObject<LoginResponse>(responseString);

                if (loginResult != null)
                {
                    if (loginResult.RolesId == 1)
                    {
                        HttpContext.Session.SetString("UserId", userId); // bạn có thể lưu thêm thông tin nếu cần
                        return Redirect("/admin/homeadmin/index");
                    }
                    else if (loginResult.RolesId == 0)
                    {
                        return Redirect("/home/index");
                    }
                }

                ViewBag.Error = "Lỗi không xác định trong dữ liệu đăng nhập.";
                return View();
            }
            else
            {
                ViewBag.Error = "Tài khoản hoặc mật khẩu không đúng.";
                return View();
            }
        }

        [HttpGet]
        public IActionResult CheckSession()
        {
            var userId = HttpContext.Session.GetString("UserId");
            return Json(new { isAuthenticated = !string.IsNullOrEmpty(userId) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            try
            {
                HttpContext.Session.Clear();

                // Xóa tất cả cookies
                foreach (var cookie in Request.Cookies.Keys)
                {
                    Response.Cookies.Delete(cookie);
                }

                // Thêm headers để đảm bảo không cache response
                Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate, private");
                Response.Headers.Add("Pragma", "no-cache");
                Response.Headers.Add("Expires", "-1");

                TempData["Message"] = "Đăng xuất thành công!";

                // Thêm cache-busting parameter
                return Redirect($"/admin/homeadmin/indexadminlogin?logout=true&t={DateTime.Now.Ticks}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logout error: {ex.Message}");
                return Redirect($"/admin/homeadmin/indexadminlogin?error=true&t={DateTime.Now.Ticks}");
            }
        }
    }
}
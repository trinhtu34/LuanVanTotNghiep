using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using testpayment6._0.Models;

namespace testpayment6._0.Areas.admin.Controllers
{
    [Area("admin")]
    public class MenuController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseApiUrl = "https://9ennjx1tb5.execute-api.ap-southeast-1.amazonaws.com/Prod/api/menu";

        public MenuController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var response = await _httpClient.GetAsync(_baseApiUrl);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var menus = JsonSerializer.Deserialize<List<Menu>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return View(menus ?? new List<Menu>());
                }
                ViewBag.Error = "Không thể tải danh sách thực đơn";
                return View(new List<Menu>());
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Lỗi: {ex.Message}";
                return View(new List<Menu>());
            }
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Menu menu)
        {
            if (string.IsNullOrWhiteSpace(menu.DishName) || menu.Price <= 0 || menu.CategoryId <= 0 || menu.RegionId <= 0)
            {
                ViewBag.Error = "Thông tin món ăn không hợp lệ";
                return View(menu);
            }
            try
            {
                var content = new StringContent(JsonSerializer.Serialize(menu), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_baseApiUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction("Index");
                }
                ViewBag.Error = "Không thể tạo món ăn mới";
                return View(menu);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Lỗi: {ex.Message}";
                return View(menu);
            }
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }
            try
            {
                var response = await _httpClient.GetAsync($"{_baseApiUrl}/{id}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var menu = JsonSerializer.Deserialize<Menu>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return View(menu);
                }
                ViewBag.Error = "Không thể tải thông tin món ăn";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Lỗi: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Menu menu)
        {
            if (string.IsNullOrWhiteSpace(menu.DishId) || string.IsNullOrWhiteSpace(menu.DishName) || menu.Price <= 0 || menu.CategoryId <= 0 || menu.RegionId <= 0)
            {
                ViewBag.Error = "Thông tin món ăn không hợp lệ";
                return View(menu);
            }
            try
            {
                var content = new StringContent(JsonSerializer.Serialize(menu), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"{_baseApiUrl}/{menu.DishId}", content);
                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction("Index");
                }
                ViewBag.Error = "Không thể cập nhật món ăn";
                return View(menu);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Lỗi: {ex.Message}";
                return View(menu);
            }
        }

    }
}

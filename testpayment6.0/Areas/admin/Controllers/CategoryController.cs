using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using testpayment6._0.Models;

namespace testpayment6._0.Areas.admin.Controllers
{
    [Area("admin")]
    public class CategoryController : Controller
    {
        private readonly HttpClient _httpClient;
        //private readonly string _baseApiUrl = "https://9ennjx1tb5.execute-api.ap-southeast-1.amazonaws.com/Prod/api/category";
        private readonly string BASE_API_URL;

        public CategoryController(HttpClient httpClient , IConfiguration configuration)
        {
            _httpClient = httpClient;
            BASE_API_URL = configuration["BaseAPI"];
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var response = await _httpClient.GetAsync(BASE_API_URL);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var categories = JsonSerializer.Deserialize<List<Category>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return View(categories ?? new List<Category>());
                }
                ViewBag.Error = "Không thể tải danh sách danh mục";
                return View(new List<Category>());
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Lỗi: {ex.Message}";
                return View(new List<Category>());
            }
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                ViewBag.Error = "Tên loại món ăn không được để trống";
                return View();
            }
            try
            {
                var categoryData = new { CategoryName = categoryName };
                var content = new StringContent(JsonSerializer.Serialize(categoryData), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(BASE_API_URL, content);
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Thêm loại món ăn thành công!";
                    return RedirectToAction("Index");
                }
                ViewBag.Error = "Không thể tạo danh mục mới";
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Lỗi: {ex.Message}";
                return View();
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/{id}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var category = JsonSerializer.Deserialize<Category>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return View(category);
                }
                ViewBag.Error = "Không thể tải danh mục";
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
        public async Task<IActionResult> Edit(int id, string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                ViewBag.Error = "Tên loại món ăn không được để trống";
                return View(new Category { CategoryId = id, CategoryName = categoryName });
            }
            try
            {
                var categoryData = new { CategoryId = id, CategoryName = categoryName };
                var content = new StringContent(JsonSerializer.Serialize(categoryData), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"{BASE_API_URL}/{id}", content);
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Cập nhật loại món ăn thành công!";
                    return RedirectToAction("Index");
                }
                ViewBag.Error = "Không thể cập nhật danh mục";
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Lỗi: {ex.Message}";
                return View();
            }
        }
    }
}

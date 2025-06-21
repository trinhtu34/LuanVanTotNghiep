using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using testpayment6._0.Areas.admin.Models;
using testpayment6._0.Models;

namespace testpayment6._0.Areas.admin.Controllers
{
    [Area("admin")]
    public class MenuController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string BASE_API_URL;

        public MenuController(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            BASE_API_URL = configuration["BaseAPI"];
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Load menu data
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/menu");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var menus = JsonSerializer.Deserialize<List<MenuAdmin>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    // Load categories và regions để map tên
                    var categories = await LoadCategories();
                    var regions = await LoadRegions();

                    // Map tên category và region vào menu
                    if (menus != null)
                    {
                        foreach (var menu in menus)
                        {
                            var category = categories.FirstOrDefault(c => c.CategoryId == menu.CategoryId);
                            var region = regions.FirstOrDefault(r => r.RegionId == menu.RegionId);

                            menu.CategoryName = category?.CategoryName ?? "Không xác định";
                            menu.RegionName = region?.RegionName ?? "Không xác định";
                        }
                    }

                    return View(menus ?? new List<MenuAdmin>());
                }
                ViewBag.Error = "Không thể tải danh sách thực đơn";
                return View(new List<MenuAdmin>());
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Lỗi: {ex.Message}";
                return View(new List<MenuAdmin>());
            }
        }

        public async Task<IActionResult> Create()
        {
            await LoadDropdownData();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MenuAdmin menu)
        {
            // Kiểm tra validation
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(menu.DishId))
                errors.Add("Mã món ăn không được để trống");

            if (string.IsNullOrWhiteSpace(menu.DishName))
                errors.Add("Tên món ăn không được để trống");

            if (menu.Price <= 0)
                errors.Add("Giá tiền phải lớn hơn 0");

            if (menu.CategoryId <= 0)
                errors.Add("Vui lòng chọn danh mục");

            if (menu.RegionId <= 0)
                errors.Add("Vui lòng chọn khu vực");

            if (errors.Any())
            {
                ViewBag.Error = "Lỗi validation: " + string.Join(", ", errors);
                await LoadDropdownData();
                return View(menu);
            }

            try
            {
                var content = new StringContent(JsonSerializer.Serialize(menu), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{BASE_API_URL}/menu", content);
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Thêm món ăn thành công!";
                    return RedirectToAction("Index");
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                ViewBag.Error = $"Không thể tạo món ăn mới. API Error: {errorContent}";
                await LoadDropdownData();
                return View(menu);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Lỗi: {ex.Message}";
                await LoadDropdownData();
                return View(menu);
            }
        }
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["Error"] = "Mã món ăn không hợp lệ";
                return RedirectToAction("Index");
            }

            try
            {
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/menu/{id}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var menu = JsonSerializer.Deserialize<MenuAdmin>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (menu == null)
                    {
                        await LoadDropdownData();
                        return View(new MenuAdmin());
                    }


                    var categories = await LoadCategories();
                    var regions = await LoadRegions();

                    var category = categories.FirstOrDefault(c => c.CategoryId == menu.CategoryId);
                    var region = regions.FirstOrDefault(r => r.RegionId == menu.RegionId);

                    menu.CategoryName = category?.CategoryName ?? "Không xác định";
                    menu.RegionName = region?.RegionName ?? "Không xác định";

                    await LoadDropdownData();
                    return View(menu);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await LoadDropdownData();
                    return View(new MenuAdmin());
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MenuAdmin menu)
        {
            // Validation
            var errors = new List<string>();


            if (string.IsNullOrWhiteSpace(menu.DishName))
                errors.Add("Tên món ăn không được để trống");

            if (menu.Price <= 0)
                errors.Add("Giá tiền phải lớn hơn 0");

            if (menu.CategoryId <= 0)
                errors.Add("Vui lòng chọn danh mục");

            if (menu.RegionId <= 0)
                errors.Add("Vui lòng chọn khu vực");

            if (errors.Any())
            {
                ViewBag.Error = "Lỗi validation: " + string.Join(", ", errors);
                await LoadDropdownData();
                return View(menu);
            }

            try
            {
                var content = new StringContent(JsonSerializer.Serialize(menu), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"{BASE_API_URL}/menu/{menu.DishId}", content);
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Cập nhật món ăn thành công!";
                    return RedirectToAction("Index");
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                ViewBag.Error = $"Không thể cập nhật món ăn. API Error: {errorContent}";
                await LoadDropdownData();
                return View(menu);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Lỗi: {ex.Message}";
                await LoadDropdownData();
                return View(menu);
            }
        }
        private async Task LoadDropdownData()
        {
            ViewBag.Regions = await LoadRegions();
            ViewBag.Categories = await LoadCategories();
        }

        private async Task<List<Region>> LoadRegions()
        {
            try
            {
                var regionResponse = await _httpClient.GetAsync($"{BASE_API_URL}/region");
                if (regionResponse.IsSuccessStatusCode)
                {
                    var regionJson = await regionResponse.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<Region>>(regionJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<Region>();
                }
            }
            catch (Exception)
            {
                // Log error if needed
            }
            return new List<Region>();
        }

        private async Task<List<Category>> LoadCategories()
        {
            try
            {
                var categoryResponse = await _httpClient.GetAsync($"{BASE_API_URL}/category");
                if (categoryResponse.IsSuccessStatusCode)
                {
                    var categoryJson = await categoryResponse.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<Category>>(categoryJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<Category>();
                }
            }
            catch (Exception)
            {
                // Log error if needed
            }
            return new List<Category>();
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using testpayment6._0.Areas.admin.Models;

namespace testpayment6._0.Areas.admin.Controllers
{
    [Area("admin")]
    public class TableController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string BASE_API_URL;

        public TableController(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            BASE_API_URL = configuration["BaseAPI"];
        }

        private async Task<List<RegionAdmin>> LoadRegions()
        {
            var response = await _httpClient.GetAsync($"{BASE_API_URL}/region");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<RegionAdmin>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<RegionAdmin>();
            }
            return new List<RegionAdmin>();
        }
        private async Task LoadDropdownData()
        {
            //var regions = await LoadRegions();
            //ViewBag.Regions = regions.Select(r => new { r.RegionId, r.RegionName }).ToList();
            ViewBag.Regions = await LoadRegions();
        }
        public async Task<IActionResult> Index()
        {
            try
            {
                // Load danh sách bàn từ API
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/table");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var tables = JsonSerializer.Deserialize<List<TableAdmin>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    var regions = await LoadRegions();
                    // Map tên khu vực vào bàn
                    if (tables != null)
                    {
                        foreach (var table in tables)
                        {
                            var region = regions.FirstOrDefault(r => r.RegionId == table.RegionId);
                            table.RegionName = region?.RegionName ?? "Không xác định";
                        }
                    }
                    return View(tables ?? new List<TableAdmin>());
                }
                ViewBag.Error = "Không thể tải danh sách bàn";
                return View(new List<TableAdmin>());
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Lỗi: {ex.Message}";
                return View(new List<TableAdmin>());
            }
        }

        public async Task<IActionResult> Create()
        {
            await LoadDropdownData();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TableAdmin table)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var jsonContent = new StringContent(JsonSerializer.Serialize(table), System.Text.Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync($"{BASE_API_URL}/table", jsonContent);
                    if (response.IsSuccessStatusCode)
                    {
                        TempData["SuccessTable"] = "Tạo bàn mới thành công!";
                        return RedirectToAction("Index");
                    }
                    ViewBag.Error = "Không thể tạo bàn mới";
                }
                catch (Exception ex)
                {
                    ViewBag.Error = $"Lỗi: {ex.Message}";
                }
            }
            await LoadDropdownData();
            return View(table);
        }

        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/table/{id}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var table = JsonSerializer.Deserialize<TableAdmin>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (table != null)
                    {
                        await LoadDropdownData();
                        return View(table);
                    }
                }
                ViewBag.Error = "Không tìm thấy bàn cần chỉnh sửa";
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
        public async Task<IActionResult> Edit(TableAdmin table)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var jsonContent = new StringContent(JsonSerializer.Serialize(table), System.Text.Encoding.UTF8, "application/json");
                    var response = await _httpClient.PutAsync($"{BASE_API_URL}/table/{table.TableId}", jsonContent);
                    if (response.IsSuccessStatusCode)
                    {
                        TempData["SuccessTable"] = "Cập nhật bàn thành công!";
                        return RedirectToAction("Index");
                    }
                    TempData["ErrorTable"] = "Không thể cập nhật bàn";
                }
                catch (Exception ex)
                {
                    ViewBag.Error = $"Lỗi: {ex.Message}";
                }
            }
            await LoadDropdownData();
            return View(table);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{BASE_API_URL}/table/{id}");
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessTable"] = "Xóa bàn thành công!";
                    return RedirectToAction("Index");
                }
                TempData["ErrorTable"] = "Không thể xóa bàn. Vui lòng thử lại.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Lỗi: {ex.Message}";
            }
            return RedirectToAction("Index");
        }
    }
}

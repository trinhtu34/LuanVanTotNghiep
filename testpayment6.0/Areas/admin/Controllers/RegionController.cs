using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using testpayment6._0.Models;

namespace testpayment6._0.Controllers
{
    [Area("admin")]
    public class RegionController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string BASE_API_URL;

        public RegionController(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            BASE_API_URL = configuration["BaseAPI"];
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/region");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var regions = JsonSerializer.Deserialize<List<Region>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return View(regions ?? new List<Region>());
                }
                ViewBag.Error = "Không thể tải danh sách khu vực";
                return View(new List<Region>());
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Lỗi: {ex.Message}";
                return View(new List<Region>());
            }
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string regionName)
        {
            if (string.IsNullOrWhiteSpace(regionName))
            {
                ViewBag.Error = "Tên khu vực không được để trống";
                return View();
            }

            try
            {
                var regionData = new { RegionName = regionName };
                var json = JsonSerializer.Serialize(regionData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{BASE_API_URL}/region", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessRegion"] = "Thêm khu vực thành công!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ViewBag.Error = "Không thể thêm khu vực. Vui lòng thử lại.";
                    return View();
                }
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
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/region");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var regions = JsonSerializer.Deserialize<List<Region>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    var region = regions?.FirstOrDefault(r => r.RegionId == id);
                    if (region != null)
                    {
                        return View(region);
                    }
                }
                ViewBag.Error = "Không tìm thấy khu vực";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Lỗi: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string regionName)
        {
            if (string.IsNullOrWhiteSpace(regionName))
            {
                ViewBag.Error = "Tên khu vực không được để trống";
                return View(new Region { RegionId = id, RegionName = regionName });
            }

            try
            {
                var regionData = new { regionId = id, regionName = regionName };
                var json = JsonSerializer.Serialize(regionData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"{BASE_API_URL}/region/{id}", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessRegion"] = "Cập nhật khu vực thành công!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ViewBag.Error = "Không thể cập nhật khu vực. Vui lòng thử lại.";
                    return View(new Region { RegionId = id, RegionName = regionName });
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Lỗi: {ex.Message}";
                return View(new Region { RegionId = id, RegionName = regionName });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{BASE_API_URL}/region/{id}");
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessRegion"] = "Xóa khu vực thành công!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorRegion"] = "Không thể xóa khu vực. Vui lòng thử lại.";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorRegion"] = $"Lỗi: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
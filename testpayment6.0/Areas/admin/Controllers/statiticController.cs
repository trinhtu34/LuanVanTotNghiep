using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Threading.Tasks;
using testpayment6._0.Areas.admin.Models;

namespace testpayment6._0.Areas.admin.Controllers
{
    [Area("admin")]
    public class statiticController : Controller
    {

        private readonly HttpClient _httpClient;
        private readonly string BASE_API_URL;

        public statiticController(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            BASE_API_URL = configuration["BaseAPI"];
        }
        public async Task<IActionResult> Index()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/ordertable/highest-revenue-table");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    
                    var highestRevenueTables = JsonSerializer.Deserialize<highest_revenue_table>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return View(highestRevenueTables ?? new highest_revenue_table());
                }
                ViewBag.ErrorHRT = "Không thể tải danh sách bàn có doanh thu cao nhất";
                return View(new highest_revenue_table());
            }
            catch (Exception ex)
            {
                ViewBag.ErrorHRT = $"Lỗi: {ex.Message}";
                return View(new highest_revenue_table());
            }
            return View();
        }
    }
}

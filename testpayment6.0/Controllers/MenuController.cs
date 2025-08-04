using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using testpayment6._0.Models;
using testpayment6._0.ResponseModels;

namespace testpayment6._0.Controllers
{
    public class MenuController : Controller
    {
        private readonly ILogger<MenuController> _logger;
        private readonly HttpClient _httpClient;
        private readonly string BASE_API_URL;

        public MenuController(ILogger<MenuController> logger, HttpClient httpClient, IConfiguration configuration)
        {
            _logger = logger;
            _httpClient = httpClient;
            BASE_API_URL = configuration["BaseAPI"];
        }
        [HttpGet]
        public async Task<IActionResult> Index(int? categoryId = null, int? regionId = null, string search = "")
        {
            try
            {
                var viewModel = new MenuViewModel_menu
                {
                    SelectedCategoryId = categoryId,
                    SelectedRegionId = regionId,
                    SearchText = search ?? ""
                };

                var dishesTask = GetDishesAsync();
                var categoriesTask = GetCategoriesAsync();
                var regionsTask = GetRegionsAsync();

                await Task.WhenAll(dishesTask, categoriesTask, regionsTask);

                viewModel.Dishes = dishesTask.Result;
                viewModel.Categories = categoriesTask.Result;
                viewModel.Regions = regionsTask.Result;

                // Áp dụng bộ lọc
                if (categoryId.HasValue)
                {
                    viewModel.Dishes = viewModel.Dishes.Where(d => d.CategoryId == categoryId.Value).ToList();
                }

                if (regionId.HasValue)
                {
                    viewModel.Dishes = viewModel.Dishes.Where(d => d.RegionId == regionId.Value).ToList();
                }

                if (!string.IsNullOrEmpty(search))
                {
                    viewModel.Dishes = viewModel.Dishes.Where(d =>
                        d.DishName.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                //ViewBag.IsLoggedIn = !string.IsNullOrEmpty(HttpContext.Session.GetString("UserId"));
                //ViewBag.UserId = HttpContext.Session.GetString("UserId");


                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading menu data");
                TempData["Error"] = "Không thể tải dữ liệu menu. Vui lòng thử lại.";
                return View(new MenuViewModel_menu());
            }
        }    

        private async Task<List<DishModels_Menu>> GetDishesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/menu/quantity/excludecount");
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    return JsonSerializer.Deserialize<List<DishModels_Menu>>(jsonContent, options) ?? new List<DishModels_Menu>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching dishes from API");
            }
            return new List<DishModels_Menu>();
        }

        private async Task<List<CategoryModels_Menu>> GetCategoriesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/category");
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    return JsonSerializer.Deserialize<List<CategoryModels_Menu>>(jsonContent, options) ?? new List<CategoryModels_Menu>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching categories from API");
            }
            return new List<CategoryModels_Menu>();
        }

        private async Task<List<RegionModels_Menu>> GetRegionsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/region");
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    return JsonSerializer.Deserialize<List<RegionModels_Menu>>(jsonContent, options) ?? new List<RegionModels_Menu>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching regions from API");
            }
            return new List<RegionModels_Menu>();
        }



    }
}
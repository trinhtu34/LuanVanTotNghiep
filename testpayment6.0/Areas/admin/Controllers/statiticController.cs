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
                var tableRevenue = await GetHighRevenueTable();
                return View(tableRevenue);
            }
            catch (Exception ex) 
            { 
                return View(new List<revenue_table>());
            }
        }

        // danh sách bàn và doanh thu 
        private async Task<List<revenue_table>> GetHighRevenueTable()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/ordertable/highest-revenue-table");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<List<revenue_table>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return data ?? new List<revenue_table>();
                }
            }
            catch
            {

            }

            return new List<revenue_table>();
        }

        //- Doanh thu món ăn ---------------------------------------------------------------------------------------------------------
        public async Task<IActionResult> SoLieuMonAn()
        {
            try
            {
                var viewModel = await GetDishRevenueAsync();
                return View(viewModel);
            }
            catch (Exception ex)
            {
                return View(new List<DishRevenueViewModel>());
            }
        }
        public async Task<List<DishRevenueViewModel>> GetDishRevenueAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/cart/dish-revenue");
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    return JsonSerializer.Deserialize<List<DishRevenueViewModel>>(jsonContent, options)
                           ?? new List<DishRevenueViewModel>();
                }
                else
                {
                    throw new Exception("Không thể tải dữ liệu doanh thu món ăn");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Có lỗi xảy ra: {ex.Message}");
            }
        }
        //public async Task<IActionResult> SoLieuMonAn(DateTime? startDate = null, DateTime? endDate = null, int? categoryId = null)
        //{
        //    var viewModel = new DishRevenueViewModel
        //    {
        //        StartDate = startDate,
        //        EndDate = endDate,
        //        CategoryId = categoryId
        //    };

        //    try
        //    {
        //        var dishRevenue = await GetDishRevenueAsync(startDate, endDate, categoryId);
        //        viewModel.DishRevenue = dishRevenue;

        //        var categories = await GetCategoriesAsync();
        //        viewModel.Categories = categories;
        //    }
        //    catch (Exception ex)
        //    {
        //        ViewBag.ErrorMessage = $"Có lỗi xảy ra: {ex.Message}";
        //    }

        //    return View(viewModel);
        //}

        //public async Task<List<DishRevenueModel>> GetDishRevenueAsync(
        //    DateTime? startDate = null,
        //    DateTime? endDate = null,
        //    int? categoryId = null)
        //{
        //    try
        //    {
        //        var queryParams = new List<string>();

        //        if (startDate.HasValue)
        //            queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
        //        if (endDate.HasValue)
        //            queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");
        //        if (categoryId.HasValue)
        //            queryParams.Add($"categoryId={categoryId.Value}");

        //        var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
        //        var response = await _httpClient.GetAsync($"{BASE_API_URL}/cart/dish-revenue{queryString}");

        //        if (response.IsSuccessStatusCode)
        //        {
        //            var jsonContent = await response.Content.ReadAsStringAsync();
        //            return JsonSerializer.Deserialize<List<DishRevenueModel>>(jsonContent) ?? new List<DishRevenueModel>();
        //        }
        //        else
        //        {
        //            throw new Exception("Không thể tải dữ liệu doanh thu món ăn");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception($"Có lỗi xảy ra: {ex.Message}");
        //    }
        //}

        public async Task<List<CategoryModel>> GetCategoriesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/category");

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<CategoryModel>>(jsonContent) ?? new List<CategoryModel>();
                }
                else
                {
                    return new List<CategoryModel>();
                }
            }
            catch (Exception)
            {
                return new List<CategoryModel>();
            }
        }
        public async Task<IActionResult> SoLieuLoaiMonAn(DateTime? startDate = null, DateTime? endDate = null)
        {
            var viewModel = new CategoryRevenueViewModel
            {
                StartDate = startDate,
                EndDate = endDate
            };

            try
            {
                var categoryRevenue = await GetCategoryRevenueAsync(startDate, endDate);
                viewModel.CategoryRevenue = categoryRevenue;

                var categories = await GetCategoriesAsync();
                viewModel.Categories = categories;
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Có lỗi xảy ra: {ex.Message}";
            }

            return View(viewModel);
        }

        public async Task<List<CategoryRevenueModel>> GetCategoryRevenueAsync(
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                var queryParams = new List<string>();
                if (startDate.HasValue)
                    queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
                if (endDate.HasValue)
                    queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");

                var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/cart/revenue-by-category{queryString}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    return JsonSerializer.Deserialize<List<CategoryRevenueModel>>(jsonContent, options)
                           ?? new List<CategoryRevenueModel>();
                }
                else
                {
                    throw new Exception("Không thể tải dữ liệu doanh thu theo danh mục");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Có lỗi xảy ra: {ex.Message}");
            }
        }
        // số liệu khu vực ----------------------------------------------------------------------
        public async Task<IActionResult> SoLieuKhuVuc(DateTime? startDate = null, DateTime? endDate = null)
        {
            var viewModel = new RegionRevenueViewModel
            {
                StartDate = startDate,
                EndDate = endDate
            };

            try
            {
                var regionrevenue = await GetRegionRevenueAsync(startDate, endDate);
                viewModel.RegionRevenue = regionrevenue;

                var regions = await GetRegionAsync();
                viewModel.regions = regions;
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessageSLKV = $"Có lỗi xảy ra: {ex.Message}";
            }

            return View(viewModel);
        }

        public async Task<List<RegionRevenueModel>> GetRegionRevenueAsync(
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                var queryParams = new List<string>();
                if (startDate.HasValue)
                    queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
                if (endDate.HasValue)
                    queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");

                var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/cart/revenue-by-region{queryString}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    return JsonSerializer.Deserialize<List<RegionRevenueModel>>(jsonContent, options)
                           ?? new List<RegionRevenueModel>();
                }
                else
                {
                    throw new Exception("Không thể tải dữ liệu doanh thu theo danh mục");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Có lỗi xảy ra: {ex.Message}");
            }
        }
        public async Task<List<Region>> GetRegionAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/region");

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<Region>>(jsonContent) ?? new List<Region>();
                }
                else
                {
                    return new List<Region>();
                }
            }
            catch (Exception)
            {
                return new List<Region>();
            }
        }

        //public async Task<IActionResult> ExportExcel(DateTime? startDate = null, DateTime? endDate = null, int? categoryId = null)
        //{
        //    try
        //    {
        //        var queryParams = new List<string>();

        //        if (startDate.HasValue)
        //            queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
        //        if (endDate.HasValue)
        //            queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");
        //        if (categoryId.HasValue)
        //            queryParams.Add($"categoryId={categoryId.Value}");

        //        var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
        //        var response = await _httpClient.GetAsync($"{BASE_API_URL}/dish-revenue/export{queryString}");

        //        if (response.IsSuccessStatusCode)
        //        {
        //            var fileBytes = await response.Content.ReadAsByteArrayAsync();
        //            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        //                       $"DishRevenue_{DateTime.Now:yyyyMMdd}.xlsx");
        //        }
        //        else
        //        {
        //            TempData["ErrorMessage"] = "Không thể xuất file Excel";
        //            return RedirectToAction("Index");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        TempData["ErrorMessage"] = $"Có lỗi xảy ra khi xuất Excel: {ex.Message}";
        //        return RedirectToAction("Index");
        //    }
        //}

    }
}

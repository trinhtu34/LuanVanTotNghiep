using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using testpayment6._0.Areas.admin.Models;

namespace testpayment6._0.Areas.admin.Controllers
{
    [Area("admin")]
    public class OrderController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OrderController> _logger;
        private string BASE_API_URL;

        public OrderController(HttpClient httpClient, ILogger<OrderController> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            BASE_API_URL = configuration["BaseAPI"];
        }

        [HttpGet]
        public async Task<IActionResult> CreateOrder()
        {
            var model = new OrderViewModel();

            try
            {
                // Lấy danh sách khu vực
                var regionsResponse = await _httpClient.GetAsync($"{BASE_API_URL}/region");
                if (regionsResponse.IsSuccessStatusCode)
                {
                    var regionsJson = await regionsResponse.Content.ReadAsStringAsync();
                    model.Regions = JsonSerializer.Deserialize<List<RegionViewModel_Guest>>(regionsJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<RegionViewModel_Guest>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading regions");
                ViewBag.Error = "Không thể tải dữ liệu khu vực";
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrder(OrderViewModel model)
        {
            try
            {
                // Kiểm tra có món ăn nào được chọn không
                if (model.SelectedDishes == null || !model.SelectedDishes.Any(d => d.Quantity > 0))
                {
                    TempData["ErrorOrder"] = "Vui lòng chọn ít nhất một món ăn!";
                    return RedirectToAction("CreateOrder");
                }

                // 1. Tạo tài khoản khách hàng
                var userId = DateTime.Now.Ticks.ToString();

                var signupRequest = new
                {
                    UserId = userId,
                    CustomerName = model.CustomerName ?? "",
                    PhoneNumber = model.PhoneNumber ?? ""
                };

                var signupJson = JsonSerializer.Serialize(signupRequest);
                var signupContent = new StringContent(signupJson, Encoding.UTF8, "application/json");
                var signupResponse = await _httpClient.PostAsync($"{BASE_API_URL}/user/signup/guest", signupContent);

                if (!signupResponse.IsSuccessStatusCode)
                {
                    TempData["ErrorOrder"] = "Không thể tạo tài khoản khách hàng";
                    return RedirectToAction("CreateOrder");
                }

                // 2. Tạo giỏ hàng
                var cartRequest = new
                {
                    UserId = userId,
                    totalPrice = model.SelectedDishes.Sum(d => d.Price * d.Quantity),
                };

                var cartJson = JsonSerializer.Serialize(cartRequest);
                var cartContent = new StringContent(cartJson, Encoding.UTF8, "application/json");
                var cartResponse = await _httpClient.PostAsync($"{BASE_API_URL}/cart", cartContent);

                if (!cartResponse.IsSuccessStatusCode)
                {
                    TempData["ErrorOrder"] = "Không thể tạo giỏ hàng";
                    return RedirectToAction("CreateOrder");
                }

                var cartResponseContent = await cartResponse.Content.ReadAsStringAsync();
                var cartInfo = JsonSerializer.Deserialize<CartResponse>(cartResponseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // 3. Thêm món ăn vào giỏ hàng
                var selectedDishes = model.SelectedDishes.Where(d => d.Quantity > 0).ToList();

                foreach (var dish in selectedDishes)
                {
                    var cartDetailRequest = new
                    {
                        CartId = cartInfo.CartId.ToString(),
                        DishId = dish.DishId,
                        Quantity = dish.Quantity.ToString(),
                        Price = dish.Price.ToString()
                    };

                    var cartDetailJson = JsonSerializer.Serialize(cartDetailRequest);
                    var cartDetailContent = new StringContent(cartDetailJson, Encoding.UTF8, "application/json");
                    var cartDetailResponse = await _httpClient.PostAsync($"{BASE_API_URL}/CartDetail", cartDetailContent);

                    if (!cartDetailResponse.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("Failed to add dish {DishId} to cart", dish.DishId);
                    }
                }

                var totalAmount = selectedDishes.Sum(d => d.Price * d.Quantity);
                var customerInfo = string.IsNullOrEmpty(model.CustomerName) ? "Khách hàng" : model.CustomerName;

                TempData["SuccessOrder"] = $"Đặt món thành công cho {customerInfo}! Tổng tiền: {totalAmount:N0} VNĐ";
                return RedirectToAction("CreateOrder");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                TempData["ErrorOrder"] = "Có lỗi xảy ra khi đặt món: " + ex.Message;
                return RedirectToAction("CreateOrder");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMenuByRegion(int regionId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/menu/region/{regionId}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var menu = JsonSerializer.Deserialize<List<MenuViewModel_Guest>>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return Json(new { success = true, data = menu });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading menu for region {regionId}");
            }

            return Json(new { success = false, message = "Không thể tải danh sách món ăn" });
        }
    }
}
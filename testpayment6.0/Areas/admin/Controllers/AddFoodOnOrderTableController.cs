using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using testpayment6._0.Areas.admin.Models;

namespace testpayment6._0.Areas.admin.Controllers
{
    [Area("admin")]
    public class AddFoodOnOrderTableController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AddFoodOnOrderTableController> _logger;
        private readonly string BASE_API_URL;

        public AddFoodOnOrderTableController(HttpClient httpClient, IConfiguration configuration, ILogger<AddFoodOnOrderTableController> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            BASE_API_URL = configuration["BaseAPI"];
        }

        [HttpGet]
        public async Task<IActionResult> Index()
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
        public async Task<IActionResult> Index(string orderTableId, List<string> dishIds, List<int> quantities, List<decimal> prices)
        {
            try
            {
                if (string.IsNullOrEmpty(orderTableId))
                {
                    TempData["ErrorOrder"] = "Vui lòng nhập mã đơn đặt bàn";
                    return RedirectToAction("Index");
                }

                if (dishIds == null || dishIds.Count == 0)
                {
                    TempData["ErrorOrder"] = "Vui lòng chọn ít nhất một món ăn";
                    return RedirectToAction("Index");
                }

                // Lấy danh sách món ăn hiện tại của đơn đặt bàn
                var existingOrdersResponse = await _httpClient.GetAsync($"https://p7igzosmei.execute-api.ap-southeast-1.amazonaws.com/Prod/api/orderfooddetail/list/{orderTableId}");
                List<OrderFoodDetailResponse> existingOrders = new List<OrderFoodDetailResponse>();

                if (existingOrdersResponse.IsSuccessStatusCode)
                {
                    var existingOrdersJson = await existingOrdersResponse.Content.ReadAsStringAsync();
                    existingOrders = JsonSerializer.Deserialize<List<OrderFoodDetailResponse>>(existingOrdersJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<OrderFoodDetailResponse>();
                }

                // Xử lý từng món ăn được chọn
                for (int i = 0; i < dishIds.Count; i++)
                {
                    if (quantities[i] <= 0) continue;

                    var dishId = dishIds[i];
                    var quantity = quantities[i];
                    var price = prices[i];

                    // Kiểm tra xem món ăn đã tồn tại trong đơn đặt bàn chưa
                    var existingOrder = existingOrders.FirstOrDefault(x => x.DishId == dishId);

                    if (existingOrder != null)
                    {
                        // Nếu đã tồn tại, cập nhật số lượng
                        await UpdateOrderFoodQuantity(existingOrder.OrderFoodDetailsId, quantity);
                    }
                    else
                    {
                        // Nếu chưa tồn tại, tạo mới
                        await CreateNewOrderFood(orderTableId, dishId, quantity, price);
                    }
                }

                // Tính lại tổng tiền
                await CalculateTotalPrice(orderTableId);

                TempData["SuccessOrder"] = "Đã thêm món ăn thành công!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                TempData["ErrorOrder"] = "Có lỗi xảy ra khi đặt món ăn";
            }

            return RedirectToAction("Index");
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

        [HttpGet]
        public async Task<IActionResult> GetOrderFoodDetails(string orderTableId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"https://p7igzosmei.execute-api.ap-southeast-1.amazonaws.com/Prod/api/orderfooddetail/list/{orderTableId}");
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var orderDetails = JsonSerializer.Deserialize<List<OrderFoodDetailResponse>>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return Json(new { success = true, data = orderDetails });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading order food details for orderTableId {orderTableId}");
            }
            return Json(new { success = false, message = "Không thể tải thông tin đơn đặt bàn" });
        }

        private async Task<bool> CreateNewOrderFood(string orderTableId, string dishId, int quantity, decimal price)
        {
            try
            {
                var orderRequest = new
                {
                    OrderTableId = orderTableId,
                    DishId = dishId,
                    Quantity = quantity.ToString(),
                    Price = price.ToString()
                };

                var json = JsonSerializer.Serialize(orderRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://p7igzosmei.execute-api.ap-southeast-1.amazonaws.com/Prod/api/OrderFoodDetail", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating new order food for dish {dishId}");
                return false;
            }
        }

        private async Task<bool> UpdateOrderFoodQuantity(int orderFoodDetailsId, int additionalQuantity)
        {
            try
            {
                var updateRequest = new
                {
                    quantity = additionalQuantity
                };

                var json = JsonSerializer.Serialize(updateRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"https://p7igzosmei.execute-api.ap-southeast-1.amazonaws.com/Prod/api/OrderFoodDetail/quantity/{orderFoodDetailsId}", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating order food quantity for orderFoodDetailsId {orderFoodDetailsId}");
                return false;
            }
        }

        private async Task<bool> CalculateTotalPrice(string orderTableId)
        {
            try
            {
                var response = await _httpClient.PutAsync($"https://p7igzosmei.execute-api.ap-southeast-1.amazonaws.com/Prod/api/ordertable/totalprice/calculate/{orderTableId}", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calculating total price for orderTableId {orderTableId}");
                return false;
            }
        }
    }

    // Models cần thiết
    public class OrderFoodDetailResponse
    {
        public int OrderFoodDetailsId { get; set; }
        public int OrderTableId { get; set; }
        public string DishId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string Note { get; set; }
    }
}
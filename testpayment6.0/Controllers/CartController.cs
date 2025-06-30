// Controllers/CartController.cs
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using testpayment6._0.ResponseModels;

namespace testpayment6._0.Controllers
{
    public class CartController : Controller
    {
        private readonly ILogger<CartController> _logger;
        private readonly HttpClient _httpClient;
        private readonly string BASE_API_URL;

        public CartController(ILogger<CartController> logger, HttpClient httpClient, IConfiguration configuration)
        {
            _logger = logger;
            _httpClient = httpClient;
            BASE_API_URL = configuration["BaseAPI"];
        }

        // Hiển thị giỏ hàng
        public IActionResult Index()
        {
            var cart = GetCartFromSession();
            ViewBag.TotalAmount = cart.Sum(x => x.Total);
            return View(cart);
        }

        // Thêm món vào giỏ hàng (AJAX)
        [HttpPost]
        public IActionResult AddToCart(string dishId, string dishName, decimal price, string image = "")
        {
            try
            {
                var cart = GetCartFromSession();
                var existingItem = cart.FirstOrDefault(x => x.DishId == dishId);

                if (existingItem != null)
                {
                    existingItem.Quantity++;
                }
                else
                {
                    cart.Add(new CartItem
                    {
                        DishId = dishId,
                        DishName = dishName,
                        Price = price,
                        Quantity = 1,
                        Image = image
                    });
                }

                SaveCartToSession(cart);

                return Json(new { success = true, cartCount = cart.Sum(x => x.Quantity) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to cart");
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
        }

        // Cập nhật số lượng
        [HttpPost]
        public IActionResult UpdateQuantity(string dishId, int quantity)
        {
            try
            {
                var cart = GetCartFromSession();
                var item = cart.FirstOrDefault(x => x.DishId == dishId);

                if (item != null)
                {
                    if (quantity > 0)
                    {
                        item.Quantity = quantity;
                    }
                    else
                    {
                        cart.Remove(item);
                    }
                }

                SaveCartToSession(cart);

                return Json(new
                {
                    success = true,
                    cartCount = cart.Sum(x => x.Quantity),
                    total = cart.Sum(x => x.Total)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart quantity");
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
        }

        // Xóa món khỏi giỏ hàng
        [HttpPost]
        public IActionResult RemoveFromCart(string dishId)
        {
            try
            {
                var cart = GetCartFromSession();
                var item = cart.FirstOrDefault(x => x.DishId == dishId);

                if (item != null)
                {
                    cart.Remove(item);
                    SaveCartToSession(cart);
                }

                return Json(new
                {
                    success = true,
                    cartCount = cart.Sum(x => x.Quantity),
                    total = cart.Sum(x => x.Total)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from cart");
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
        }

        // Đặt hàng - Lưu vào database
        [HttpPost]
        public async Task<IActionResult> PlaceOrder()
        {
            try
            {
                var userId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập" });
                }

                var cart = GetCartFromSession();
                if (!cart.Any())
                {
                    return Json(new { success = false, message = "Giỏ hàng trống" });
                }

                // Tạo cart trong database
                var cartResponse = await CreateCartAsync(userId);
                if (cartResponse == null)
                {
                    return Json(new { success = false, message = "Không thể tạo đơn hàng" });
                }

                // Thêm từng món vào cart detail
                bool allItemsAdded = true;
                foreach (var item in cart)
                {
                    var success = await AddCartDetailAsync(cartResponse.CartId.ToString(), item.DishId,
                        item.Quantity.ToString(), item.Price.ToString());
                    if (!success)
                    {
                        allItemsAdded = false;
                        break;
                    }
                }

                if (allItemsAdded)
                {
                    // Xóa giỏ hàng session sau khi đặt hàng thành công
                    HttpContext.Session.Remove("Cart");
                    return Json(new { success = true, message = "Đặt hàng thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Có lỗi khi thêm món vào đơn hàng" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error placing order");
                return Json(new { success = false, message = "Có lỗi xảy ra khi đặt hàng" });
            }
        }

        // Lấy số lượng items trong giỏ hàng (để hiển thị badge)
        [HttpGet]
        public IActionResult GetCartCount()
        {
            var cart = GetCartFromSession();
            return Json(cart.Sum(x => x.Quantity));
        }

        // Helper methods
        private List<CartItem> GetCartFromSession()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            if (string.IsNullOrEmpty(cartJson))
            {
                return new List<CartItem>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
            }
            catch
            {
                return new List<CartItem>();
            }
        }

        private void SaveCartToSession(List<CartItem> cart)
        {
            var cartJson = JsonSerializer.Serialize(cart);
            HttpContext.Session.SetString("Cart", cartJson);
        }

        private async Task<CartResponseModel> CreateCartAsync(string userId)
        {
            try
            {
                var request = new CreateCartRequest { UserId = userId };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{BASE_API_URL}/cart", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    return JsonSerializer.Deserialize<CartResponseModel>(responseJson, options);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating cart");
            }
            return null;
        }

        private async Task<bool> AddCartDetailAsync(string cartId, string dishId, string quantity, string price)
        {
            try
            {
                var request = new AddCartDetailRequest
                {
                    CartId = cartId,
                    DishId = dishId,
                    Quantity = quantity,
                    Price = price
                };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{BASE_API_URL}/CartDetail", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding cart detail");
                return false;
            }
        }
    }
    public class CartItem
    {
        public string DishId { get; set; } // Keep this definition
        public string DishName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string Image { get; set; }
        public decimal Total => Quantity * Price;
    }
}
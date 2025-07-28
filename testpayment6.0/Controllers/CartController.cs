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
        public IActionResult Index()
        {
            var cart = GetCartFromSession();
            ViewBag.TotalAmount = cart.Sum(x => x.Total);

            ViewBag.UserId = HttpContext.Session.GetString("UserId");
            ViewBag.IsLoggedIn = !string.IsNullOrEmpty(ViewBag.UserId);

            return View(cart);
        }
        [HttpPost]
        public IActionResult AddToCart(string dishId, string dishName, decimal price, string image = "")
        {
            _logger.LogInformation("AddToCart called with: dishId={DishId}, dishName={DishName}, price={Price}",
                                  dishId, dishName, price);

            var sessionId = HttpContext.Session.Id;
            _logger.LogInformation("Session ID: {SessionId}", sessionId);

            try
            {
                var cart = GetCartFromSession();
                _logger.LogInformation("Current cart has {Count} items", cart.Count);

                var existingItem = cart.FirstOrDefault(x => x.DishId == dishId);

                if (existingItem != null)
                {
                    existingItem.Quantity++;
                    _logger.LogInformation("Updated existing item quantity to {Quantity}", existingItem.Quantity);
                }
                else
                {
                    var newItem = new CartItem
                    {
                        DishId = dishId,
                        DishName = dishName,
                        Price = price,
                        Quantity = 1,
                        Image = image
                    };
                    cart.Add(newItem);
                    _logger.LogInformation("Added new item to cart: {DishName}", dishName);
                }

                SaveCartToSession(cart);

                var totalCount = cart.Sum(x => x.Quantity);
                _logger.LogInformation("Cart saved. Total count: {TotalCount}", totalCount);

                return Json(new { success = true, cartCount = totalCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to cart");
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

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

                var totalPrice = cart.Sum(x => x.Total ?? 0);

                var cartResponse = await CreateCartAsync(userId, totalPrice);
                if (cartResponse == null)
                {
                    return Json(new { success = false, message = "Không thể tạo đơn hàng" });
                }

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
                    HttpContext.Session.SetString("CartId", cartResponse.CartId.ToString());
                    HttpContext.Session.SetString("CartTotalPrice", totalPrice.ToString());

                    return Json(new
                    {
                        success = true,
                        message = "Đặt hàng thành công!",
                        cartId = cartResponse.CartId,
                        totalPrice = totalPrice,
                        redirectToPayment = true
                    });
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

        [HttpGet]
        public IActionResult GetCartCount()
        {
            var cart = GetCartFromSession();
            return Json(cart.Sum(x => x.Quantity));
        }

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

        private async Task<CartResponseModel> CreateCartAsync(string userId, decimal totalPrice)
        {
            try
            {
                _logger.LogInformation("CreateCartAsync called with userId: {UserId}, totalPrice: {TotalPrice}",
                    userId, totalPrice);
                _logger.LogInformation("BASE_API_URL: {BaseApiUrl}", BASE_API_URL ?? "NULL");

                var request = new CreateCartRequest
                {
                    UserId = userId,
                    TotalPrice = totalPrice
                };

                var json = JsonSerializer.Serialize(request);
                _logger.LogInformation("Request JSON: {Json}", json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{BASE_API_URL}/cart", content);

                _logger.LogInformation("API Response - Status: {StatusCode}, IsSuccess: {IsSuccess}",
                    response.StatusCode, response.IsSuccessStatusCode);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("API Response JSON: {ResponseJson}", responseJson);

                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var result = JsonSerializer.Deserialize<CartResponseModel>(responseJson, options);

                    _logger.LogInformation("Deserialized result - CartId: {CartId}", result?.CartId);
                    return result;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("API Error - Status: {StatusCode}, Content: {ErrorContent}",
                        response.StatusCode, errorContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in CreateCartAsync");
            }
            return null;
        }

        private async Task<bool> AddCartDetailAsync(string cartId, string dishId, string quantity, string price)
        {
            try
            {
                _logger.LogInformation("AddCartDetailAsync called - CartId: {CartId}, DishId: {DishId}, Quantity: {Quantity}, Price: {Price}",
                    cartId, dishId, quantity, price);

                var request = new AddCartDetailRequest
                {
                    CartId = cartId,
                    DishId = dishId,
                    Quantity = quantity,
                    Price = price
                };

                var json = JsonSerializer.Serialize(request);
                _logger.LogInformation("CartDetail Request JSON: {Json}", json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{BASE_API_URL}/CartDetail", content);

                _logger.LogInformation("CartDetail API Response - Status: {StatusCode}, IsSuccess: {IsSuccess}",
                    response.StatusCode, response.IsSuccessStatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("CartDetail API Error - Content: {ErrorContent}", errorContent);
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in AddCartDetailAsync");
                return false;
            }
        }
    }
}
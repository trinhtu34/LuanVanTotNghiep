using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using testpayment6._0.Areas.admin.Models;
using testpayment6._0.Models;
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

            var sessionId = HttpContext.Session.Id;

            try
            {
                var cart = GetCartFromSession();
                _logger.LogInformation("Current cart has {Count} items", cart.Count);

                var existingItem = cart.FirstOrDefault(x => x.DishId == dishId);

                if (existingItem != null)
                {
                    existingItem.Quantity++;
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
                }

                SaveCartToSession(cart);

                var totalCount = cart.Sum(x => x.Quantity);

                return Json(new { success = true, cartCount = totalCount });
            }
            catch (Exception ex)
            {
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
                    return Json(new { success = false, message = "Vui lòng đăng nhập hoặc nhập thông tin cá nhân" });
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
                    // Clear cart sau khi đặt hàng thành công
                    HttpContext.Session.Remove("Cart");

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
                _logger.LogError(ex, "Error in PlaceOrder");
                return Json(new { success = false, message = "Có lỗi xảy ra khi đặt hàng" });
            }
        }
        [HttpPost]
        public async Task<IActionResult> PlaceGuestOrder(string customerName, string phoneNumber, string email, string address)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(customerName) ||
                    string.IsNullOrWhiteSpace(phoneNumber) ||
                    string.IsNullOrWhiteSpace(email) ||
                    string.IsNullOrWhiteSpace(address))
                {
                    return Json(new { success = false, message = "Vui lòng điền đầy đủ thông tin" });
                }

                var cart = GetCartFromSession();
                if (!cart.Any())
                {
                    return Json(new { success = false, message = "Giỏ hàng trống" });
                }

                var totalPrice = cart.Sum(x => x.Total ?? 0);

                // Tạo guest user
                var userResponse = await createGuestUser(customerName, phoneNumber, address, email);
                if (userResponse == null)
                {
                    return Json(new { success = false, message = "Không thể tạo thông tin khách hàng" });
                }

                // Tạo cart
                var cartResponse = await CreateCartAsync(userResponse.UserId, totalPrice);
                if (cartResponse == null)
                {
                    return Json(new { success = false, message = "Không thể tạo đơn hàng" });
                }

                // Thêm chi tiết đơn hàng
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
                    // Clear cart sau khi đặt hàng thành công
                    HttpContext.Session.Remove("Cart");

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
                _logger.LogError(ex, "Error in PlaceGuestOrder");
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
        private async Task<GuestUser_orderfood> createGuestUser(string customername, string phonenumber, string addressguest, string email)
        {
            var userid = DateTime.Now.Ticks.ToString();

            var signupRequest = new GuestUser_orderfood
            {
                UserId = userid,
                CustomerName = customername,
                PhoneNumber = phonenumber,
                address = addressguest,
                email = email
            };
            var signupJson = JsonSerializer.Serialize(signupRequest);
            var signupContent = new StringContent(signupJson, Encoding.UTF8, "application/json");
            var signupResponse = await _httpClient.PostAsync($"{BASE_API_URL}/user/signup/guest", signupContent);
            _logger.LogInformation("Signup Response - Status: {StatusCode}, IsSuccess: {IsSuccess}",
                signupResponse.StatusCode, signupResponse.IsSuccessStatusCode);
            if (signupResponse.IsSuccessStatusCode)
            {
                var responseJson = await signupResponse.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<GuestUser_orderfood>(responseJson, options);
                return result;
            }
            else
            {
                var errorContent = await signupResponse.Content.ReadAsStringAsync();
                _logger.LogError("Signup API Error - Status: {StatusCode}, Content: {ErrorContent}",
                    signupResponse.StatusCode, errorContent);
            }
            return null;

        }
        private async Task<CartResponseModel> CreateCartAsync(string userId, decimal totalPrice)
        {
            try
            {
                var request = new CreateCartRequest
                {
                    UserId = userId,
                    TotalPrice = totalPrice
                };

                var json = JsonSerializer.Serialize(request);

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{BASE_API_URL}/cart", content);

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
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using testpayment6._0.Models;
using testpayment6._0.ResponseModels;

namespace testpayment6._0.Controllers
{
    public class OrderController : Controller
    {
        private readonly ILogger<OrderController> _logger;
        private readonly HttpClient _httpClient;
        private readonly string BASE_API_URL;

        public OrderController(ILogger<OrderController> logger, HttpClient httpClient, IConfiguration configuration)
        {
            _logger = logger;
            _httpClient = httpClient;
            BASE_API_URL = configuration["BaseAPI"];
        }

        // Trang danh sách đơn hàng
        public async Task<IActionResult> Index()
        {
            // Kiểm tra đăng nhập
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login", "Home");
            }

            var userId = HttpContext.Session.GetString("UserId");

            try
            {
                // Lấy danh sách đơn hàng từ API
                var cartResponse = await _httpClient.GetAsync($"{BASE_API_URL}/cart/user/{userId}");

                if (cartResponse.IsSuccessStatusCode)
                {
                    var cartContent = await cartResponse.Content.ReadAsStringAsync();

                    _logger.LogInformation($"Successfully retrieved orders for user {cartContent}");
                    var carts = JsonSerializer.Deserialize<List<CartViewModel>>(cartContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    // Lấy thông tin thanh toán cho từng đơn hàng với logic mới
                    foreach (var cart in carts)
                    {
                        cart.PaymentStatus = await GetPaymentStatusAsync(cart.CartId);
                    }

                    var model = new OrderListViewModel
                    {
                        UserId = userId,
                        Orders = carts ?? new List<CartViewModel>()
                    };

                    return View(model);
                }
                else
                {
                    _logger.LogError($"Failed to get orders for user {userId}: {cartResponse.StatusCode}");
                    ViewBag.Error = "Không thể tải danh sách đơn hàng. Vui lòng thử lại.";
                    return View(new OrderListViewModel { UserId = userId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting orders for user {userId}");
                ViewBag.Error = "Đã xảy ra lỗi khi tải danh sách đơn hàng. Vui lòng thử lại.";
                return View(new OrderListViewModel { UserId = userId });
            }
        }

        // Trang chi tiết đơn hàng
        public async Task<IActionResult> Details(int cartId)
        {
            // Kiểm tra đăng nhập
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login", "Home");
            }

            var userId = HttpContext.Session.GetString("UserId");

            try
            {
                // Lấy thông tin đơn hàng
                var cartResponse = await _httpClient.GetAsync($"{BASE_API_URL}/cart/user/{userId}");
                CartViewModel order = null;

                if (cartResponse.IsSuccessStatusCode)
                {
                    var cartContent = await cartResponse.Content.ReadAsStringAsync();
                    var carts = JsonSerializer.Deserialize<List<CartViewModel>>(cartContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    order = carts?.FirstOrDefault(c => c.CartId == cartId);
                }

                if (order == null)
                {
                    return NotFound("Không tìm thấy đơn hàng.");
                }

                // Lấy chi tiết đơn hàng
                var detailResponse = await _httpClient.GetAsync($"{BASE_API_URL}/CartDetail/cart/{cartId}");
                List<CartDetailViewModel> orderDetails = new List<CartDetailViewModel>();

                if (detailResponse.IsSuccessStatusCode)
                {
                    var detailContent = await detailResponse.Content.ReadAsStringAsync();
                    orderDetails = JsonSerializer.Deserialize<List<CartDetailViewModel>>(detailContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<CartDetailViewModel>();
                }

                // Lấy thông tin thanh toán với logic mới
                PaymentStatusViewModel paymentStatus = await GetPaymentStatusAsync(cartId);

                var model = new OrderDetailViewModel
                {
                    Order = order,
                    OrderDetails = orderDetails,
                    PaymentStatus = paymentStatus
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting order details for cart {cartId}");
                ViewBag.Error = "Đã xảy ra lỗi khi tải chi tiết đơn hàng. Vui lòng thử lại.";
                return View(new OrderDetailViewModel());
            }
        }

        // Phương thức helper để lấy trạng thái thanh toán
        private async Task<PaymentStatusViewModel> GetPaymentStatusAsync(int cartId)
        {
            var paymentStatus = new PaymentStatusViewModel { CartId = cartId, IsSuccess = false };

            try
            {
                var paymentResponse = await _httpClient.GetAsync($"{BASE_API_URL}/Payment/cart/status/{cartId}");
                if (paymentResponse.IsSuccessStatusCode)
                {
                    var paymentContent = await paymentResponse.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Payment status response for cart {cartId}: {paymentContent}");

                    var paymentStatuses = JsonSerializer.Deserialize<List<PaymentStatusViewModel>>(paymentContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    // Logic kiểm tra: nếu có ít nhất một payment status là true thì đơn hàng đã thanh toán
                    if (paymentStatuses != null && paymentStatuses.Any(p => p.IsSuccess))
                    {
                        paymentStatus.IsSuccess = true;
                        // Lấy thông tin từ payment status đầu tiên có IsSuccess = true
                        var successPayment = paymentStatuses.First(p => p.IsSuccess);
                        paymentStatus.PaymentDate = successPayment.PaymentDate;
                        paymentStatus.PaymentMethod = successPayment.PaymentMethod;
                        paymentStatus.Amount = successPayment.Amount;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Could not get payment status for cart {cartId}");
            }

            return paymentStatus;
        }

        // Helper method để kiểm tra trạng thái đăng nhập
        private bool IsUserLoggedIn()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var isAuthenticated = HttpContext.Session.GetString("IsAuthenticated");

            return !string.IsNullOrEmpty(userId) && isAuthenticated == "true";
        }
    }
}
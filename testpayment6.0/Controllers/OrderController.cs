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
                // Lấy thống kê từ API
                var statistics = await GetStatisticsAsync(userId);

                // Lấy danh sách đơn hàng từ API
                var cartResponse = await _httpClient.GetAsync($"{BASE_API_URL}/cart/user/{userId}");

                if (cartResponse.IsSuccessStatusCode)
                {
                    var cartContent = await cartResponse.Content.ReadAsStringAsync();
                    var carts = JsonSerializer.Deserialize<List<CartViewModel>>(cartContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    // Lấy thông tin thanh toán cho từng đơn hàng
                    foreach (var cart in carts)
                    {
                        cart.PaymentStatus = await GetPaymentStatusAsync(cart.CartId);
                    }

                    var model = new OrderListViewModel
                    {
                        UserId = userId,
                        Orders = carts ?? new List<CartViewModel>(),
                        Statistics = statistics
                    };

                    return View(model);
                }
                else
                {
                    _logger.LogError($"Failed to get orders for user {userId}: {cartResponse.StatusCode}");
                    ViewBag.Error = "Không thể tải danh sách đơn hàng. Vui lòng thử lại.";
                    return View(new OrderListViewModel { UserId = userId, Statistics = statistics });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting orders for user {userId}");
                ViewBag.Error = "Đã xảy ra lỗi khi tải danh sách đơn hàng. Vui lòng thử lại.";
                return View(new OrderListViewModel { UserId = userId });
            }
        }
        private async Task<StatisticsViewModel> GetStatisticsAsync(string userId)
        {
            var statistics = new StatisticsViewModel();

            try
            {
                // api 1: Tổng số đơn hàng
                var totalResponse = await _httpClient.GetAsync($"{BASE_API_URL}/cart/user/count/{userId}");
                if (totalResponse.IsSuccessStatusCode)
                {
                    var totalContent = await totalResponse.Content.ReadAsStringAsync();
                    statistics.TotalOrders = int.Parse(totalContent);
                }

                // api 2: Số đơn đã thanh toán
                var paidResponse = await _httpClient.GetAsync($"{BASE_API_URL}/cart/user/paid/count/{userId}");
                if (paidResponse.IsSuccessStatusCode)
                {
                    var paidContent = await paidResponse.Content.ReadAsStringAsync();
                    statistics.PaidOrders = int.Parse(paidContent);
                }

                // api 3: Số đơn chưa thanh toán
                var unpaidResponse = await _httpClient.GetAsync($"{BASE_API_URL}/cart/user/unpaid/count/{userId}");
                if (unpaidResponse.IsSuccessStatusCode)
                {
                    var unpaidContent = await unpaidResponse.Content.ReadAsStringAsync();
                    statistics.UnpaidOrders = int.Parse(unpaidContent);
                }

                // api 4: Tổng tiền tất cả đơn hàng
                var totalPriceResponse = await _httpClient.GetAsync($"{BASE_API_URL}/cart/user/totalprice/{userId}");
                if (totalPriceResponse.IsSuccessStatusCode)
                {
                    var totalPriceContent = await totalPriceResponse.Content.ReadAsStringAsync();
                    statistics.TotalAmount = decimal.Parse(totalPriceContent);
                }

                // api 5: Tổng tiền đơn hàng đã thanh toán
                var paidAmountResponse = await _httpClient.GetAsync($"{BASE_API_URL}/cart/user/totalprice/paid/{userId}");
                if (paidAmountResponse.IsSuccessStatusCode)
                {
                    var paidAmountContent = await paidAmountResponse.Content.ReadAsStringAsync();
                    statistics.PaidAmount = decimal.Parse(paidAmountContent);
                }

                // api 6: Tổng tiền đơn hàng chưa thanh toán
                var unpaidAmountResponse = await _httpClient.GetAsync($"{BASE_API_URL}/cart/user/totalprice/unpaid/{userId}");
                if (unpaidAmountResponse.IsSuccessStatusCode)
                {
                    var unpaidAmountContent = await unpaidAmountResponse.Content.ReadAsStringAsync();
                    statistics.UnpaidAmount = decimal.Parse(unpaidAmountContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting statistics for user {userId}");
            }

            return statistics;
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
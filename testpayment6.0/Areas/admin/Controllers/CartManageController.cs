using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using testpayment6._0.Areas.admin.Models;

namespace testpayment6._0.Areas.admin.Controllers
{
    [Area("Admin")]
    public class CartManageController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string BASE_API_URL;

        public CartManageController(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            BASE_API_URL = configuration["BaseAPI"];
        }

        public async Task<IActionResult> Index(string filter = "current")
        {
            var viewModel = new CartViewModel_manage
            {
                FilterType = filter
            };

            try
            {
                if (filter == "current")
                {
                    viewModel.Carts = await GetCurrentCartsAsync();
                }
                else if (filter == "paid-unfinished") 
                {
                    var allCarts = await GetAllCartsAsync();
                    viewModel.Carts = allCarts.Where(c => c.IsPaid && !c.IsFinish && !c.IsCancel).ToList();
                }
                else
                {
                    viewModel.Carts = await GetAllCartsAsync();
                }

                // tính toán cho các số liệu thống kê
                viewModel.TotalOrders = viewModel.Carts.Count; // lấy tất cả số đơn hàng bao gồm : đã hủy + chưa hủy
                viewModel.TotalRevenue = viewModel.Carts.Sum(c => c.TotalPrice); // lấy tổng số tiền của tất cả đơn hàng 
                viewModel.PaidOrders = viewModel.Carts.Count(c => c.IsPaid && !c.IsCancel); // đếm số đơn đã thanh toán và chưa hủy
                viewModel.UnpaidOrders = viewModel.Carts.Count(c => !c.IsPaid && !c.IsCancel); // đếm số đơn chưa thanh toán và chưa hủy
                viewModel.CancelledOrders = viewModel.Carts.Count(c => c.IsCancel); // đếm số đơn đã hủy
                viewModel.FinishedOrders = viewModel.Carts.Count(c => c.IsFinish); // đếm số đơn đã hoàn thành
                viewModel.PaidFinishedOrders = viewModel.Carts
                    .Count(c => c.IsPaid && c.IsFinish && !c.IsCancel); // đếm số đơn đã thanh toán , đã hoàn thành , chưa hủy

                viewModel.UnpaidRevenue = viewModel.Carts
                    .Where(c => !c.IsPaid && !c.IsCancel)
                    .Sum(c => c.TotalPrice); // tổng tiền của các đơn chưa thanh toán và chưa hủy

                viewModel.PaidUnfinishedOrders = viewModel.Carts
                    .Count(c => c.IsPaid && !c.IsFinish && !c.IsCancel); // đếm số đơn đã thanh toán , chưa hoàn thành , chưa hủy
                viewModel.PaidUnfinishedRevenue = viewModel.Carts
                    .Where(c => c.IsPaid && !c.IsFinish && !c.IsCancel)
                    .Sum(c => c.TotalPrice); // tổng tiền của các đơn đã thanh toán , chưa hoàn thành , chưa hủy

                viewModel.PaidFinishedRevenue = viewModel.Carts
                    .Where(c => c.IsPaid && c.IsFinish)
                    .Sum(c => c.TotalPrice); // tổng tiền của các đơn đã thanh toán , đã hoàn thành 

                return View(viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Có lỗi xảy ra khi tải dữ liệu. Vui lòng thử lại.";
                return View(viewModel);
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var cart = await GetCartWithDetailsAsync(id);

                if (cart == null)
                {
                    return NotFound();
                }

                return View(cart);
            }
            catch (Exception ex)
            {
                // Log error
                ViewBag.ErrorMessage = "Có lỗi xảy ra khi tải chi tiết đơn đặt món ăn.";
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCartDetails(int cartId)
        {
            try
            {
                var cartDetails = await GetCartDetailsAsync(cartId);

                return Json(new { success = true, data = cartDetails });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải chi tiết đơn đặt món ăn." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CancelCart([FromBody] CancelCartRequest request)
        {
            try
            {
                var content = new StringContent(
                    JsonSerializer.Serialize(new { IsCancel = true }),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PutAsync($"{BASE_API_URL}/cart/state/{request.CartId}", content);

                if (response.IsSuccessStatusCode)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Hủy đơn thành công"
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Không thể hủy đơn. Lỗi API: {response.StatusCode}"
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Có lỗi không mong muốn xảy ra khi hủy đơn"
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsFinished([FromBody] int cartId)
        {
            try
            {
                var content = new StringContent(
                    JsonSerializer.Serialize(new { IsFinish = true }),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PutAsync($"{BASE_API_URL}/cart/stateFinish/{cartId}", content);

                if (response.IsSuccessStatusCode)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Đánh dấu hoàn thành thành công"
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Không thể cập nhật trạng thái. Lỗi API: {response.StatusCode}"
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Có lỗi không mong muốn xảy ra"
                });
            }
        }

        // Private methods
        private async Task<List<Cart_manage>> GetCurrentCartsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/cart/afterCurrentOrderTime");
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<List<Cart_manage>>(jsonString, options) ?? new List<Cart_manage>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting current carts: {ex.Message}");
                return new List<Cart_manage>();
            }
        }

        private async Task<List<Cart_manage>> GetAllCartsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/cart");
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<List<Cart_manage>>(jsonString, options) ?? new List<Cart_manage>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting all carts: {ex.Message}");
                return new List<Cart_manage>();
            }
        }

        private async Task<List<CartDetail_manage>> GetCartDetailsAsync(int cartId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/CartDetail/cart/{cartId}");
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<List<CartDetail_manage>>(jsonString, options) ?? new List<CartDetail_manage>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting cart details: {ex.Message}");
                return new List<CartDetail_manage>();
            }
        }

        private async Task<Cart_manage> GetCartWithDetailsAsync(int cartId)
        {
            try
            {
                // Get basic cart info first
                var allCarts = await GetAllCartsAsync();
                var cart = allCarts.FirstOrDefault(c => c.CartId == cartId);

                if (cart == null)
                    return null;

                // Get cart details
                cart.CartDetails = await GetCartDetailsAsync(cartId);

                return cart;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting cart with details: {ex.Message}");
                return null;
            }
        }
    }
}

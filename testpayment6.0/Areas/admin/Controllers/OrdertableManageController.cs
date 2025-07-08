using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using testpayment6._0.Areas.admin.Models;
using testpayment6._0.ResponseModels;

namespace RestaurantAdmin.Controllers
{
    [Area("Admin")]
    public class OrdertableManageController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "https://p7igzosmei.execute-api.ap-southeast-1.amazonaws.com/Prod/api";

        public OrdertableManageController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IActionResult> Index(string filter = "current")
        {
            var viewModel = new OrderTableViewModel_manage
            {
                FilterType = filter
            };

            try
            {
                if (filter == "current")
                {
                    viewModel.OrderTables = await GetCurrentOrderTablesAsync();
                }
                // cái này là filter == "all" thì sẽ lấy tất cả các đơn đặt bàn đang có trong hệ thống
                else
                {
                    viewModel.OrderTables = await GetAllOrderTablesAsync();
                }

                // Calculate statistics
                viewModel.TotalOrders = viewModel.OrderTables.Count;
                viewModel.TotalRevenue = viewModel.OrderTables.Sum(o => o.TotalPrice + o.TotalDeposit);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                // Log error
                ViewBag.ErrorMessage = "Có lỗi xảy ra khi tải dữ liệu. Vui lòng thử lại.";
                return View(viewModel);
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var orderTable = await GetOrderTableWithDetailsAsync(id);

                if (orderTable == null)
                {
                    return NotFound();
                }

                return View(orderTable);
            }
            catch (Exception ex)
            {
                // Log error
                ViewBag.ErrorMessage = "Có lỗi xảy ra khi tải chi tiết đơn đặt bàn.";
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderTableDetails(int orderTableId)
        {
            try
            {
                var tableDetails = await GetOrderTableDetailsAsync(orderTableId);
                var foodDetails = await GetOrderFoodDetailsAsync(orderTableId);

                var result = new
                {
                    tableDetails = tableDetails,
                    foodDetails = foodDetails
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = "Có lỗi xảy ra khi tải chi tiết đơn đặt bàn." });
            }
        }
        // phương thức để lấy dữ liệu cho các đơn từ 1 tiếng trước tới sau này -- tùy theo lựa chọn bộ lọc 
        private async Task<List<OrderTable_manage>> GetCurrentOrderTablesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/ordertable/afterCurrentStartingTime");
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<List<OrderTable_manage>>(jsonString, options) ?? new List<OrderTable_manage>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting current order tables: {ex.Message}");
                return new List<OrderTable_manage>();
            }
        }

        // lấy tất cả các đơn đặt bàn đang có - dùng cho bộ lọc lấy hết thông tin trong hệ thống 
        private async Task<List<OrderTable_manage>> GetAllOrderTablesAsync()
        {
            try
            {
                //var response = await _httpClient.GetAsync($"{_baseUrl}/ordertable");
                var response = await _httpClient.GetAsync($"{_baseUrl}/ordertable/paymentstatus");
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<List<OrderTable_manage>>(jsonString, options) ?? new List<OrderTable_manage>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting all order tables: {ex.Message}");
                return new List<OrderTable_manage>();
            }
        }

        // lấy chi tiết các bàn của 1 đơn đặt bàn khi ấn ào xem chi tiết
        private async Task<List<OrderTableDetail_manage>> GetOrderTableDetailsAsync(int orderTableId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/OrderTablesDetail/list/{orderTableId}");
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<List<OrderTableDetail_manage>>(jsonString, options) ?? new List<OrderTableDetail_manage>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting order table details: {ex.Message}");
                return new List<OrderTableDetail_manage>();
            }
        }

        // lấy chi tiết món ăn của 1 đơn đặt bàn khi ấn vào xem chi tiết
        private async Task<List<OrderFoodDetail_manage>> GetOrderFoodDetailsAsync(int orderTableId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/OrderFoodDetail/list/{orderTableId}");
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<List<OrderFoodDetail_manage>>(jsonString, options) ?? new List<OrderFoodDetail_manage>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting order food details: {ex.Message}");
                return new List<OrderFoodDetail_manage>();
            }
        }
        // phương thức để lấy chi tiết đơn cho trang details
        private async Task<OrderTable_manage> GetOrderTableWithDetailsAsync(int orderTableId)
        {
            try
            {
                // Get basic order info first
                var allOrders = await GetAllOrderTablesAsync();
                var orderTable = allOrders.FirstOrDefault(o => o.OrderTableId == orderTableId);

                if (orderTable == null)
                    return null;

                // Get table details
                orderTable.OrderTableDetails = await GetOrderTableDetailsAsync(orderTableId);

                // Get food details
                orderTable.OrderFoodDetails = await GetOrderFoodDetailsAsync(orderTableId);

                return orderTable;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting order table with details: {ex.Message}");
                return null;
            }
        }
        [HttpPost]
        public async Task<IActionResult> CancelOrder([FromBody] CancelOrderRequest request)
        {
            try
            {
                var content = new StringContent(
                    JsonSerializer.Serialize(new { IsCancel = true }),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PutAsync($"{_baseUrl}/OrderTable/state/{request.OrderTableId}", content);

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
                    var errorContent = await response.Content.ReadAsStringAsync();

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
    }
}
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using testpayment6._0.Areas.admin.Models;

namespace testpayment6._0.Areas.admin.Controllers
{
    [Area("admin")]
    public class BookingController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BookingController> _logger;
        private string BASE_API_URL;

        public BookingController(HttpClient httpClient, ILogger<BookingController> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            BASE_API_URL = configuration["BaseAPI"];
        }

        public async Task<IActionResult> Index()
        {
            var model = await LoadBookingData();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CreateBooking()
        {
            _logger.LogInformation("GET CreateBooking called");

            var model = await LoadBookingData();

            // khởi tạo SelectedDishes , dùng cho chọn món ăn ở frontend 
            model.SelectedDishes = new List<SelectedDish>();

            _logger.LogInformation("Initialized empty SelectedDishes list");

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBooking(BookingViewModel model)
        {
            // Log chi tiết các bàn được chọn
            if (model.SelectedTableIds != null && model.SelectedTableIds.Any())
            {
                _logger.LogInformation("Selected tables: {Tables}", string.Join(", ", model.SelectedTableIds));
            }

            // Log chi tiết các món ăn được chọn
            if (model.SelectedDishes != null && model.SelectedDishes.Any())
            {
                foreach (var dish in model.SelectedDishes.Where(d => d != null && d.Quantity > 0))
                {
                    _logger.LogInformation("Selected dish: {DishName} (ID: {DishId}) - Quantity: {Quantity}, Price: {Price}",
                        dish.DishName, dish.DishId, dish.Quantity, dish.Price);
                }
            }

            // Kiểm tra có bàn nào được chọn không
            if (model.SelectedTableIds == null || !model.SelectedTableIds.Any())
            {
                TempData["ErrorCB"] = "Vui lòng chọn ít nhất một bàn!";
                var reloadModel = await LoadBookingData();
                reloadModel.CustomerName = model.CustomerName;
                reloadModel.PhoneNumber = model.PhoneNumber;
                reloadModel.startingTime = model.startingTime;
                return View(reloadModel);
            }

            // Filter selected dishes (only those with quantity > 0)
            var selectedDishes = model.SelectedDishes?.Where(d => d != null && d.Quantity > 0).ToList();

            try
            {
                // Validate thời gian đăng ký
                var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                if (!DateTime.TryParse(model.startingTime.ToString(), out DateTime parsedStartTime))
                {
                    TempData["ErrorCB"] = "Thời gian không hợp lệ";
                    var reloadModel = await LoadBookingData();
                    reloadModel.CustomerName = model.CustomerName;
                    reloadModel.PhoneNumber = model.PhoneNumber;
                    reloadModel.startingTime = model.startingTime;
                    return View(reloadModel);
                }

                DateTime nowInVietnam = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);
                if (parsedStartTime <= nowInVietnam)
                {
                    TempData["ErrorCB"] = "Thời gian phải trong tương lai";
                    var reloadModel = await LoadBookingData();
                    reloadModel.CustomerName = model.CustomerName;
                    reloadModel.PhoneNumber = model.PhoneNumber;
                    reloadModel.startingTime = model.startingTime;
                    return View(reloadModel);
                }

                // Kiểm tra thời gian và bàn trống
                var conflictResult = await CheckTableAvailability(model.SelectedTableIds, parsedStartTime);
                if (!conflictResult.IsAvailable)
                {
                    TempData["ErrorCB"] = conflictResult.ErrorMessage;
                    var reloadModel = await LoadBookingData();
                    reloadModel.CustomerName = model.CustomerName;
                    reloadModel.PhoneNumber = model.PhoneNumber;
                    reloadModel.startingTime = model.startingTime;
                    reloadModel.SelectedTableIds = model.SelectedTableIds;
                    return View(reloadModel);
                }

                //1. Tạo tài khoản
                var userId = DateTime.Now.Ticks.ToString();

                var signupRequest = new UserSignupRequest
                {
                    UserId = userId,
                    CustomerName = model.CustomerName,
                    PhoneNumber = model.PhoneNumber
                };

                var signupJson = JsonSerializer.Serialize(signupRequest);
                var signupContent = new StringContent(signupJson, Encoding.UTF8, "application/json");
                var signupResponse = await _httpClient.PostAsync($"{BASE_API_URL}/user/signup/guest", signupContent);

                var signupResponseContent = await signupResponse.Content.ReadAsStringAsync();
                _logger.LogInformation("Signup response: {Response}", signupResponseContent);

                if (!signupResponse.IsSuccessStatusCode)
                {
                    TempData["ErrorCB"] = "Không thể tạo tài khoản khách hàng";
                    return RedirectToAction("CreateBooking");
                }

                var userInfo = JsonSerializer.Deserialize<userResponse>(signupResponseContent);
                string useridString = userInfo.userId;

                decimal totalPrice = 0;
                if (selectedDishes != null && selectedDishes.Any())
                {
                    foreach (var dish in selectedDishes)
                    {
                        decimal dishTotal = dish.Price * dish.Quantity;
                        totalPrice += dishTotal;

                        _logger.LogInformation("Selected dish: {DishName} (ID: {DishId}) - Quantity: {Quantity}, Price: {Price:N0} VNĐ, Subtotal: {Subtotal:N0} VNĐ",
                            dish.DishName, dish.DishId, dish.Quantity, dish.Price, dishTotal);
                    }
                }
                // 2. Tạo đơn đặt bàn
                var orderRequest = new OrderTableRequest
                {
                    userId = useridString,
                    isCancel = false,
                    totalPrice = totalPrice,
                    startingTime = parsedStartTime
                };

                var orderJson = JsonSerializer.Serialize(orderRequest);
                var orderContent = new StringContent(orderJson, Encoding.UTF8, "application/json");
                var orderResponse = await _httpClient.PostAsync($"{BASE_API_URL}/OrderTable", orderContent);

                if (!orderResponse.IsSuccessStatusCode)
                {
                    TempData["ErrorCB"] = "Không thể tạo đơn đặt bàn";
                    return RedirectToAction("CreateBooking");
                }

                var orderResponseContent = await orderResponse.Content.ReadAsStringAsync();
                _logger.LogInformation("Order table response: {Response}", orderResponseContent);

                var orderTableResponse = JsonSerializer.Deserialize<OrderTableResponse>(orderResponseContent);
                var orderTableId = orderTableResponse.orderTableId;

                // 3. Thêm TẤT CẢ các bàn được chọn vào đơn đặt bàn
                _logger.LogInformation("Adding {Count} tables to order...", model.SelectedTableIds.Count);
                foreach (var tableId in model.SelectedTableIds)
                {
                    _logger.LogInformation("Adding table {TableId} to order...", tableId);
                    var tableDetailRequest = new OrderTableDetailRequest
                    {
                        orderTableId = orderTableId,
                        TableId = tableId.ToString()
                    };

                    var tableDetailJson = JsonSerializer.Serialize(tableDetailRequest);
                    var tableDetailContent = new StringContent(tableDetailJson, Encoding.UTF8, "application/json");
                    var tableDetailResponse = await _httpClient.PostAsync($"{BASE_API_URL}/OrderTablesDetail", tableDetailContent);

                    if (!tableDetailResponse.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("Failed to add table {TableId} to order", tableId);
                    }
                }

                // 4. Thêm món ăn vào đơn đặt bàn
                if (selectedDishes != null && selectedDishes.Any())
                {
                    _logger.LogInformation("Adding {Count} dishes to order...", selectedDishes.Count);
                    foreach (var dish in selectedDishes)
                    {
                        _logger.LogInformation("Adding dish: {DishId} - {DishName} x {Quantity}",
                            dish.DishId, dish.DishName, dish.Quantity);

                        var foodDetailRequest = new OrderFoodDetailRequest
                        {
                            OrderTableId = orderTableId,
                            DishId = dish.DishId,
                            Quantity = dish.Quantity,
                            Price = dish.Price
                        };

                        var foodDetailJson = JsonSerializer.Serialize(foodDetailRequest);
                        var foodDetailContent = new StringContent(foodDetailJson, Encoding.UTF8, "application/json");
                        var foodResponse = await _httpClient.PostAsync($"{BASE_API_URL}/OrderFoodDetail", foodDetailContent);

                        if (!foodResponse.IsSuccessStatusCode)
                        {
                            _logger.LogWarning("Failed to add dish {DishId} to order", dish.DishId);
                        }
                    }
                }

                var tableNames = string.Join(", ", model.SelectedTableIds.Select(id => $"Bàn {id}"));
                var dishSummary = selectedDishes != null && selectedDishes.Any()
                    ? $" và {selectedDishes.Count} món ăn"
                    : "";

                TempData["SuccessCB"] = $"Đặt bàn thành công cho khách hàng {model.CustomerName}! Các bàn: {tableNames}{dishSummary}";
                return RedirectToAction("CreateBooking");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating booking");
                TempData["ErrorCB"] = "Có lỗi xảy ra khi đặt bàn: " + ex.Message;
                return RedirectToAction("CreateBooking");
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetRegions()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/region");
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var regions = JsonSerializer.Deserialize<List<RegionViewModel>>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return Json(new { success = true, data = regions });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading regions");
            }
            return Json(new { success = false, message = "Không thể tải danh sách khu vực" });
        }
        [HttpGet]
        public async Task<IActionResult> GetTablesByRegion(int regionId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/table/region/{regionId}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var tables = JsonSerializer.Deserialize<List<TablesViewModel>>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return Json(new { success = true, data = tables });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading tables for region {regionId}");
            }

            return Json(new { success = false, message = "Không thể tải danh sách bàn" });
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
                    var menu = JsonSerializer.Deserialize<List<MenuViewModel>>(jsonContent, new JsonSerializerOptions
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

        // Phương thức kiểm tra bàn trống
        private async Task<(bool IsAvailable, string ErrorMessage)> CheckTableAvailability(List<int> tableIds, DateTime requestedTime)
        {
            try
            {
                // Lấy danh sách tất cả đơn đặt bàn
                var ordersResponse = await _httpClient.GetAsync($"{BASE_API_URL}/ordertable/afterCurrentStartingTime");
                if (!ordersResponse.IsSuccessStatusCode)
                {
                    return (false, "Không thể kiểm tra tình trạng bàn");
                }

                var ordersJson = await ordersResponse.Content.ReadAsStringAsync();
                var allOrders = JsonSerializer.Deserialize<List<OrderTableResponse>>(ordersJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<OrderTableResponse>();

                // Lọc các đơn chưa bị hủy
                var activeOrders = allOrders.Where(o => !o.isCancel).ToList();

                // Lấy chi tiết bàn cho tất cả đơn đặt bàn active
                var occupiedTables = new List<(int tableId, DateTime startTime)>();

                foreach (var order in activeOrders)
                {
                    var detailResponse = await _httpClient.GetAsync($"{BASE_API_URL}/OrderTablesDetail/list/{order.orderTableId}");
                    if (detailResponse.IsSuccessStatusCode)
                    {
                        var detailJson = await detailResponse.Content.ReadAsStringAsync();
                        _logger.LogInformation("Table details response for order {OrderId}: {Response}", order.orderTableId, detailJson);

                        var tableDetails = JsonSerializer.Deserialize<List<OrderTableDetailApiResponse>>(detailJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }) ?? new List<OrderTableDetailApiResponse>();

                        foreach (var detail in tableDetails)
                        {
                            occupiedTables.Add((detail.tableId, order.startingTime));
                            _logger.LogInformation("Found occupied table: {TableId} at {StartTime}", detail.tableId, order.startingTime);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Failed to get table details for order {OrderId}. Status: {Status}",
                            order.orderTableId, detailResponse.StatusCode);
                    }
                }

                // Kiểm tra conflict cho từng bàn được chọn
                var conflictTables = new List<int>();

                foreach (var tableId in tableIds)
                {
                    var conflicts = occupiedTables.Where(ot => ot.tableId == tableId).ToList();

                    foreach (var (_, existingStartTime) in conflicts)
                    {
                        // Kiểm tra khoảng thời gian 2 tiếng trước và sau
                        var timeDifference = Math.Abs((requestedTime - existingStartTime).TotalHours);

                        _logger.LogInformation("Checking conflict for table {TableId}: requested time {RequestedTime}, existing time {ExistingTime}, difference {TimeDifference} hours",
                            tableId, requestedTime, existingStartTime, timeDifference);

                        if (timeDifference < 2)
                        {
                            conflictTables.Add(tableId);
                            _logger.LogWarning("Table {TableId} has conflict: time difference {TimeDifference} hours", tableId, timeDifference);
                            break;
                        }
                    }
                }

                if (conflictTables.Any())
                {
                    var conflictTableNames = string.Join(", ", conflictTables.Select(id => $"Bàn {id}"));
                    return (false, $"Các bàn sau không thể đặt vì có đơn khác trong vòng 2 tiếng: {conflictTableNames}");
                }

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking table availability");
                return (false, "Có lỗi khi kiểm tra tình trạng bàn: " + ex.Message);
            }
        }
        private async Task<BookingViewModel> LoadBookingData()
        {
            var model = new BookingViewModel();

            try
            {
                _logger.LogInformation("Loading booking data...");

                // Lấy danh sách khu vực
                var regionsResponse = await _httpClient.GetAsync($"{BASE_API_URL}/region");
                if (regionsResponse.IsSuccessStatusCode)
                {
                    var regionsJson = await regionsResponse.Content.ReadAsStringAsync();
                    model.Regions = JsonSerializer.Deserialize<List<RegionViewModel>>(regionsJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<RegionViewModel>();

                    _logger.LogInformation("Loaded {Count} regions", model.Regions.Count);
                }

                // Lấy danh sách bàn
                var tablesResponse = await _httpClient.GetAsync($"{BASE_API_URL}/table");
                if (tablesResponse.IsSuccessStatusCode)
                {
                    var tablesJson = await tablesResponse.Content.ReadAsStringAsync();
                    model.Tables = JsonSerializer.Deserialize<List<TableModel>>(tablesJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<TableModel>();

                    _logger.LogInformation("Loaded {Count} tables", model.Tables.Count);
                }
                else
                {
                    _logger.LogWarning("Failed to load tables. Status: {Status}", tablesResponse.StatusCode);
                }

                // Lấy danh sách món ăn
                var menuResponse = await _httpClient.GetAsync($"{BASE_API_URL}/menu");
                if (menuResponse.IsSuccessStatusCode)
                {
                    var menuJson = await menuResponse.Content.ReadAsStringAsync();
                    model.Dishes = JsonSerializer.Deserialize<List<DishModel>>(menuJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<DishModel>();

                    _logger.LogInformation("Loaded {Count} dishes", model.Dishes.Count);
                }
                else
                {
                    _logger.LogWarning("Failed to load dishes. Status: {Status}", menuResponse.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading booking data");
                ViewBag.ErrorCB = "Không thể tải dữ liệu: " + ex.Message;
            }

            return model;
        }
    }
}
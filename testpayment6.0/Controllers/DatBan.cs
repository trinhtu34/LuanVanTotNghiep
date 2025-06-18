using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using testpayment6._0.Models;
using testpayment6._0.ResponseModels;

namespace testpayment6._0.Controllers
{
    public class DatBan : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DatBan> _logger;
        private readonly string BASE_API_URL;

        public DatBan(HttpClient httpClient, ILogger<DatBan> logger , IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            BASE_API_URL = configuration["BaseAPI"];
        }

        public async Task<IActionResult> Index()
        {
            // kiểm tra đăng nhập , ý là bắt phải đăng nhập mới vào index của đặt bàn đc 
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Vui lòng đăng nhập để đặt bàn";
                return RedirectToAction("Login", "Home");
            }

            try
            {
                // Lấy danh sách tất cả bàn
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/table");

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var tables = JsonSerializer.Deserialize<List<TablesViewModel>>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (tables != null && tables.Any())
                    {
                        // Nhóm bàn theo region
                        var groupedTables = tables.GroupBy(t => t.RegionId)
                                                 .OrderBy(g => g.Key)
                                                 .ToList();
                        ViewBag.GroupedTables = groupedTables;
                        ViewBag.UserId = userId;

                        // Lấy danh sách các region để load menu
                        var regionIds = groupedTables.Select(g => g.Key).ToList();
                        ViewBag.RegionIds = regionIds;
                    }
                    else
                    {
                        ViewBag.GroupedTables = new List<IGrouping<int, TablesViewModel>>();
                        TempData["Info"] = "Hiện tại không có bàn nào khả dụng";
                    }
                }
                else
                {
                    TempData["Error"] = "Không thể tải danh sách bàn. Vui lòng thử lại sau.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra khi tải danh sách bàn";
            }

            return View();
        }

        // kiểm tra bàn xem là có ai đặt trong vòng 2 tiếng hay chưa
        private async Task<List<int>> CheckTableAvailabilityAsync(List<int> tableIds, DateTime startingTime)
        {
            var unavailableTables = new List<int>();

            try
            {
                // Lấy tất cả đơn đặt bàn
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/ordertable");

                if (!response.IsSuccessStatusCode)
                {
                    return unavailableTables; // Trả về danh sách rỗng nếu không lấy được dữ liệu
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                var allOrders = JsonSerializer.Deserialize<List<OrderTableResponse>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (allOrders == null || !allOrders.Any())
                {
                    return unavailableTables; // Không có đơn nào, tất cả bàn đều khả dụng
                }

                // Lọc các đơn chưa hủy và trong khung thời gian xung đột ( khoảng 2 giờ)
                var conflictingOrders = allOrders.Where(order =>
                    !order.IsCancel && // Đơn chưa bị hủy
                    DateTime.TryParse(order.StartingTime, out DateTime orderStartTime) &&
                    Math.Abs((orderStartTime - startingTime).TotalHours) <= 2
                ).ToList();

                if (!conflictingOrders.Any())
                {
                    return unavailableTables; // Không có đơn xung đột
                }
                // Kiểm tra từng đơn xung đột để lấy danh sách bàn đặt trong từng đơn
                foreach (var order in conflictingOrders)
                {
                    try
                    {
                        var detailResponse = await _httpClient.GetAsync($"{BASE_API_URL}/OrderTablesDetail/list/{order.OrderTableId}");

                        if (detailResponse.IsSuccessStatusCode)
                        {
                            var detailJson = await detailResponse.Content.ReadAsStringAsync();
                            var orderDetails = JsonSerializer.Deserialize<List<OrderTableDetailViewModel>>(detailJson, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                            if (orderDetails != null)
                            {
                                // tìm mấy cái bàn bị trùng với cái bàn mà người dùng đang đặt không 
                                var conflictingTableIds = orderDetails
                                    .Where(detail => tableIds.Contains(detail.TableId))
                                    .Select(detail => detail.TableId)
                                    .ToList();

                                unavailableTables.AddRange(conflictingTableIds);

                                if (conflictingTableIds.Any())
                                {
                                    _logger.LogInformation($"Order {order.OrderTableId} conflicts with tables: [{string.Join(", ", conflictingTableIds)}]");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error checking order details for order {order.OrderTableId}");
                    }
                }

                // Loại bỏ duplicate
                unavailableTables = unavailableTables.Distinct().ToList();

                if (unavailableTables.Any())
                {
                    _logger.LogInformation($"Unavailable tables: [{string.Join(", ", unavailableTables)}]");
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking table availability");
            }

            return unavailableTables;
        }

        [HttpPost]
        public async Task<IActionResult> DatBanFunction(List<int> tableIds, string startingTime, decimal totalDeposit, List<OrderFoodRequest> selectedFoods = null)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }
            // Validate input
            if (tableIds == null || !tableIds.Any())
            {
                return Json(new { success = false, message = "Vui lòng chọn ít nhất một bàn" });
            }

            if (tableIds.Any(id => id <= 0))
            {
                return Json(new { success = false, message = "Thông tin bàn không hợp lệ" });
            }

            if (string.IsNullOrEmpty(startingTime))
            {
                return Json(new { success = false, message = "Vui lòng chọn thời gian bắt đầu" });
            }

            if (!DateTime.TryParse(startingTime, out DateTime parsedStartTime))
            {
                return Json(new { success = false, message = "Thời gian không hợp lệ" });
            }

            if (parsedStartTime <= DateTime.Now)
            {
                return Json(new { success = false, message = "Thời gian phải trong tương lai" });
            }

            try
            {
                // Kiểm tra xem bàn có khả dụng không 
                var unavailableTables = await CheckTableAvailabilityAsync(tableIds, parsedStartTime);

                if (unavailableTables.Any())
                {
                    var unavailableTablesList = string.Join(", ", unavailableTables);
                    return Json(new
                    {
                        success = false,
                        message = $"Các bàn sau đây đã được đặt trong khung thời gian này: {unavailableTablesList}. Vui lòng chọn bàn khác hoặc thời gian khác."
                    });
                }

                decimal totalFoodPrice = 0;
                if (selectedFoods != null && selectedFoods.Any())
                {
                    totalFoodPrice = selectedFoods.Sum(food => food.Price * food.Quantity);
                }

                // Bước 1: Tạo OrderTable
                var orderTableRequest = new
                {
                    userId = userId,
                    startingTime = parsedStartTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                    isCancel = false,
                    totalPrice = totalFoodPrice,
                    totalDeposit = totalDeposit,
                    orderDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")
                };

                var json = JsonSerializer.Serialize(orderTableRequest);

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var orderTableResponse = await _httpClient.PostAsync($"{BASE_API_URL}/OrderTable", content);


                if (orderTableResponse.IsSuccessStatusCode)
                {
                    var orderTableJson = await orderTableResponse.Content.ReadAsStringAsync();

                    var orderTable = JsonSerializer.Deserialize<OrderTableResponse>(orderTableJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (orderTable != null && orderTable.OrderTableId > 0)
                    {
                        // Bước 2: Thêm từng bàn vào OrderTable
                        var successfulTables = new List<int>();
                        var failedTables = new List<int>();

                        foreach (var tableId in tableIds)
                        {
                            try
                            {
                                var orderTableDetailRequest = new
                                {
                                    OrderTableId = orderTable.OrderTableId.ToString(),
                                    TableId = tableId.ToString()
                                };

                                var detailJson = JsonSerializer.Serialize(orderTableDetailRequest);

                                var detailContent = new StringContent(detailJson, Encoding.UTF8, "application/json");
                                var detailResponse = await _httpClient.PostAsync($"{BASE_API_URL}/OrderTablesDetail", detailContent);


                                if (detailResponse.IsSuccessStatusCode)
                                {
                                    var detailResponseContent = await detailResponse.Content.ReadAsStringAsync();
                                    successfulTables.Add(tableId);
                                }
                                else
                                {
                                    var errorContent = await detailResponse.Content.ReadAsStringAsync();
                                    failedTables.Add(tableId);
                                }
                            }
                            catch (Exception ex)
                            {
                                failedTables.Add(tableId);
                            }
                        }

                        // Bước 3: Thêm món ăn nếu có
                        var successfulFoods = new List<string>();
                        var failedFoods = new List<string>();

                        if (selectedFoods != null && selectedFoods.Any())
                        {
                            foreach (var food in selectedFoods)
                            {
                                try
                                {
                                    var orderFoodRequest = new
                                    {
                                        OrderTableId = orderTable.OrderTableId,
                                        DishId = food.DishId,
                                        Quantity = food.Quantity,
                                        Price = food.Price
                                    };

                                    var foodJson = JsonSerializer.Serialize(orderFoodRequest);
                                    var foodContent = new StringContent(foodJson, Encoding.UTF8, "application/json");
                                    var foodResponse = await _httpClient.PostAsync($"{BASE_API_URL}/OrderFoodDetail", foodContent);

                                    if (foodResponse.IsSuccessStatusCode)
                                    {
                                        successfulFoods.Add(food.DishId);
                                    }
                                    else
                                    {
                                        failedFoods.Add(food.DishId);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    failedFoods.Add(food.DishId);
                                    _logger.LogError(ex, $"Error adding food {food.DishId}");
                                }
                            }
                        }

                        // Kiểm tra kết quả
                        if (successfulTables.Any())
                        {
                            var message = $"Đặt bàn thành công cho {successfulTables.Count} bàn!";
                            if (failedTables.Any())
                            {
                                message += $" Không thể đặt {failedTables.Count} bàn: [{string.Join(", ", failedTables)}]";
                            }
                            if (successfulFoods.Any())
                            {
                                message += $" Đã thêm {successfulFoods.Count} món ăn.";
                            }
                            if (failedFoods.Any())
                            {
                                message += $" Không thể thêm {failedFoods.Count} món ăn.";
                            }

                            return Json(new
                            {
                                success = true,
                                message = message,
                                orderTableId = orderTable.OrderTableId,
                                successfulTables = successfulTables,
                                failedTables = failedTables,
                                successfulFoods = successfulFoods,
                                failedFoods = failedFoods
                            });
                        }
                        else
                        {
                            return Json(new { success = false, message = "Không thể đặt bất kỳ bàn nào. Vui lòng thử lại." });
                        }
                    }
                    else
                    {
                        return Json(new { success = false, message = "Phản hồi từ server không hợp lệ" });
                    }
                }
                else
                {
                    var errorContent = await orderTableResponse.Content.ReadAsStringAsync();
                    return Json(new { success = false, message = $"Không thể tạo đơn đặt bàn: {errorContent}" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        public async Task<IActionResult> DanhSachDatBan()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Vui lòng đăng nhập để xem danh sách đặt bàn";
                return RedirectToAction("Login", "Home");
            }

            try
            {
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/ordertable/{userId}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var orders = JsonSerializer.Deserialize<List<OrderTableViewModel>>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return View(orders ?? new List<OrderTableViewModel>());
                }
                else
                {
                    TempData["Error"] = "Không thể tải danh sách đặt bàn";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra khi tải danh sách đặt bàn";
            }

            return View(new List<OrderTableViewModel>());
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
        [HttpGet]
        public async Task<IActionResult> GetOrderFoodDetails(long orderTableId)
        {
            try
            {
                // Lấy danh sách món ăn đã đặt
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/OrderFoodDetail/list/{orderTableId}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var orderFoodDetails = JsonSerializer.Deserialize<List<dynamic>>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    var foodList = new List<object>();

                    if (orderFoodDetails != null && orderFoodDetails.Any())
                    {
                        foreach (var food in orderFoodDetails)
                        {
                            try
                            {
                                var dishId = food.GetProperty("dishId").GetString();
                                var quantity = food.GetProperty("quantity").GetInt32();

                                // Lấy thông tin chi tiết món ăn
                                var menuResponse = await _httpClient.GetAsync($"{BASE_API_URL}/menu/{dishId}");

                                if (menuResponse.IsSuccessStatusCode)
                                {
                                    var menuJson = await menuResponse.Content.ReadAsStringAsync();
                                    var menuItem = JsonSerializer.Deserialize<dynamic>(menuJson, new JsonSerializerOptions
                                    {
                                        PropertyNameCaseInsensitive = true
                                    });

                                    var dishName = menuItem.GetProperty("dishName").GetString();

                                    foodList.Add(new
                                    {
                                        dishId = dishId,
                                        dishName = dishName,
                                        quantity = quantity
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Error getting dish details for order {orderTableId}");
                            }
                        }
                    }

                    return Json(new
                    {
                        success = true,
                        data = foodList,
                        count = foodList.Count
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = "Không thể tải dữ liệu món ăn từ API"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting order food details for order {orderTableId}");
                return Json(new
                {
                    success = false,
                    message = "Có lỗi không mong muốn xảy ra"
                });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetOrderTableDetails(long orderTableId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/OrderTablesDetail/list/{orderTableId}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();

                    var details = JsonSerializer.Deserialize<List<OrderTableDetailViewModel>>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return Json(new
                    {
                        success = true,
                        data = details,
                        count = details?.Count ?? 0
                    });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();

                    return Json(new
                    {
                        success = false,
                        message = $"Không thể tải dữ liệu từ API. Status: {response.StatusCode}"
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

        [HttpGet]
        public async Task<IActionResult> GetPaymentStatus(long orderTableId)
        {
            try
            {
                // Gọi API kiểm tra trạng thái thanh toán
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/Payment/ordertable/status/{orderTableId}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();

                    // Log để debug
                    _logger.LogInformation($"Payment API response for order {orderTableId}: {jsonContent}");

                    // Kiểm tra nếu response rỗng hoặc là mảng rỗng
                    if (string.IsNullOrWhiteSpace(jsonContent) || jsonContent.Trim() == "[]")
                    {
                        _logger.LogInformation($"Empty response for order {orderTableId} - assuming not paid");
                        return Json(new
                        {
                            success = true,
                            data = new
                            {
                                orderTableId = orderTableId,
                                isSuccess = false,
                                isPaid = false
                            }
                        });
                    }

                    try
                    {
                        var paymentStatus = JsonSerializer.Deserialize<List<dynamic>>(jsonContent, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (paymentStatus != null && paymentStatus.Any())
                        {
                            var firstPayment = paymentStatus.First();
                            var isSuccess = firstPayment.GetProperty("isSuccess").GetBoolean();

                            _logger.LogInformation($"Payment status for order {orderTableId}: isSuccess = {isSuccess}");

                            return Json(new
                            {
                                success = true,
                                data = new
                                {
                                    orderTableId = orderTableId,
                                    isSuccess = isSuccess,
                                    isPaid = isSuccess
                                }
                            });
                        }
                        else
                        {
                            _logger.LogInformation($"No payment data found for order {orderTableId}");
                            return Json(new
                            {
                                success = true,
                                data = new
                                {
                                    orderTableId = orderTableId,
                                    isSuccess = false,
                                    isPaid = false
                                }
                            });
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, $"JSON parsing error for order {orderTableId}. Raw response: {jsonContent}");

                        // Thử parse như object đơn thay vì array
                        try
                        {
                            var singlePayment = JsonSerializer.Deserialize<dynamic>(jsonContent, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                            var isSuccess = singlePayment.GetProperty("isSuccess").GetBoolean();

                            return Json(new
                            {
                                success = true,
                                data = new
                                {
                                    orderTableId = orderTableId,
                                    isSuccess = isSuccess,
                                    isPaid = isSuccess
                                }
                            });
                        }
                        catch
                        {
                            return Json(new
                            {
                                success = true,
                                data = new
                                {
                                    orderTableId = orderTableId,
                                    isSuccess = false,
                                    isPaid = false
                                }
                            });
                        }
                    }
                }
                else
                {
                    _logger.LogWarning($"API call failed for order {orderTableId}. Status: {response.StatusCode}");
                    return Json(new
                    {
                        success = false,
                        message = $"API trả về status code: {response.StatusCode}"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting payment status for order {orderTableId}");
                return Json(new
                {
                    success = false,
                    message = "Có lỗi không mong muốn xảy ra khi kiểm tra trạng thái thanh toán"
                });
            }
        }
    }
}
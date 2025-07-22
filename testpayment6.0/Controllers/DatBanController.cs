using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.X509;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using testpayment6._0.Models;
using testpayment6._0.ResponseModels;

namespace testpayment6._0.Controllers
{
    public class DatBanController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DatBanController> _logger;
        private readonly string BASE_API_URL;

        public DatBanController(HttpClient httpClient, ILogger<DatBanController> logger, IConfiguration configuration)
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
                TempData["ErrorDB"] = "Vui lòng đăng nhập để đặt bàn";
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
                    TempData["ErrorDB"] = "Không thể tải danh sách bàn. Vui lòng thử lại sau.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorDB"] = "Có lỗi xảy ra khi tải danh sách bàn";
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
                //var response = await _httpClient.GetAsync($"{BASE_API_URL}/ordertable/afterStartingTime2MinutesAgo");
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/ordertable/afterStartingTime2HoursAgo");

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
                    !order.IsCancel &&
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
        public async Task<IActionResult> GetBookedTables()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/ordertable/afterStartingTime2MinutesAgo");
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var orders = JsonSerializer.Deserialize<List<OrderTableResponse>>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    var bookedTables = new List<int>();
                    if (orders != null && orders.Any())
                    {
                        foreach(var order in orders.Where(o => !o.IsCancel))
                        {
                            var detailResponse = await _httpClient.GetAsync($"{BASE_API_URL}/OrderTablesDetail/list/{order.OrderTableId}");
                            if (detailResponse.IsSuccessStatusCode)
                            {
                                var detailJson = await detailResponse.Content.ReadAsStringAsync();
                                var orderDetails = JsonSerializer.Deserialize<List<OrderTableDetailViewModel>>(detailJson, new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true
                                });
                                if (orderDetails != null && orderDetails.Any())
                                {
                                    bookedTables.AddRange(orderDetails.Select(d => d.TableId));
                                }
                            }
                        }
                    }
                    return Json (new { success = true, data = bookedTables.Distinct().ToList() }); 
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading booked tables");
            }
            return Json (new {success = false , message = "Không thể tải danh sách bàn đã đặt" });
        }


        // Hiển thị danh sách đặt bàn của người dùng --------------------------------------------------------------------------------------------------------------------------
        public async Task<IActionResult> DanhSachDatBan()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorDSDB"] = "Vui lòng đăng nhập để xem danh sách đặt bàn";
                return RedirectToAction("Login", "Home");
            }

            try
            {
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/ordertable/includepaymentstatus/{userId}");

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
                    TempData["ErrorDSDB"] = "Không thể tải danh sách đặt bàn";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorDSDB"] = "Có lỗi xảy ra khi tải danh sách đặt bàn";
            }

            return View(new List<OrderTableViewModel>());
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

                var response = await _httpClient.PutAsync($"{BASE_API_URL}/OrderTable/state/{request.OrderTableId}", content);

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
                    _logger.LogError($"API error when canceling order {request.OrderTableId}: {response.StatusCode} - {errorContent}");

                    return Json(new
                    {
                        success = false,
                        message = $"Không thể hủy đơn. Lỗi API: {response.StatusCode}"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error canceling order {request.OrderTableId}");
                return Json(new
                {
                    success = false,
                    message = "Có lỗi không mong muốn xảy ra khi hủy đơn"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderStatistics()
        {
            var userId = HttpContext.Session.GetString("UserId");
            _logger.LogInformation("UserId from session: {UserId}", userId);

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("UserId is null or empty - user not logged in");
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            try
            {
                _logger.LogInformation("Starting API calls for userId: {UserId}", userId);
                _logger.LogDebug("BASE_API_URL: {BaseApiUrl}", BASE_API_URL);

                // Log các URL được gọi
                var totalUrl = $"{BASE_API_URL}/ordertable/user/count/{userId}";
                var paidUrl = $"{BASE_API_URL}/ordertable/user/paid/count/{userId}";
                var unpaidUrl = $"{BASE_API_URL}/ordertable/user/unpaid/count/{userId}";
                var canceledUrl = $"{BASE_API_URL}/ordertable/canceled/{userId}";

                _logger.LogDebug("API URLs - Total: {TotalUrl}, Paid: {PaidUrl}, Unpaid: {UnpaidUrl}, Canceled: {CanceledUrl}",
                    totalUrl, paidUrl, unpaidUrl, canceledUrl);

                var totalTask = _httpClient.GetAsync(totalUrl);
                var paidTask = _httpClient.GetAsync(paidUrl);
                var unpaidTask = _httpClient.GetAsync(unpaidUrl);
                var canceledTask = _httpClient.GetAsync(canceledUrl);

                _logger.LogInformation("Starting parallel API calls for order statistics");
                await Task.WhenAll(totalTask, paidTask, unpaidTask, canceledTask);
                _logger.LogInformation("All API calls completed for user {UserId}", userId);

                var totalResponse = await totalTask;
                var paidResponse = await paidTask;
                var unpaidResponse = await unpaidTask;
                var canceledResponse = await canceledTask;

                // Log status codes
                _logger.LogDebug("API Response Status - Total: {TotalStatus}, Paid: {PaidStatus}, Unpaid: {UnpaidStatus}, Canceled: {CanceledStatus}",
                    totalResponse.StatusCode, paidResponse.StatusCode, unpaidResponse.StatusCode, canceledResponse.StatusCode);

                if (totalResponse.IsSuccessStatusCode && paidResponse.IsSuccessStatusCode &&
                    unpaidResponse.IsSuccessStatusCode && canceledResponse.IsSuccessStatusCode)
                {
                    var totalContentRaw = await totalResponse.Content.ReadAsStringAsync();
                    var paidContentRaw = await paidResponse.Content.ReadAsStringAsync();
                    var unpaidContentRaw = await unpaidResponse.Content.ReadAsStringAsync();
                    var canceledContentRaw = await canceledResponse.Content.ReadAsStringAsync();

                    // Log raw responses
                    _logger.LogDebug("Raw API responses - Total: '{TotalContent}', Paid: '{PaidContent}', Unpaid: '{UnpaidContent}', Canceled: '{CanceledContent}'",
                        totalContentRaw, paidContentRaw, unpaidContentRaw, canceledContentRaw);

                    try
                    {
                        var totalCount = int.Parse(totalContentRaw);
                        var paidCount = int.Parse(paidContentRaw);
                        var unpaidCount = int.Parse(unpaidContentRaw);
                        var canceledCount = int.Parse(canceledContentRaw);

                        _logger.LogInformation("Successfully parsed order statistics for user {UserId} - Total: {Total}, Paid: {Paid}, Unpaid: {Unpaid}, Canceled: {Canceled}",
                            userId, totalCount, paidCount, unpaidCount, canceledCount);

                        var result = new
                        {
                            success = true,
                            data = new
                            {
                                total = totalCount,
                                paid = paidCount,
                                unpaid = unpaidCount,
                                canceled = canceledCount
                            }
                        };

                        return Json(result);
                    }
                    catch (FormatException parseEx)
                    {
                        _logger.LogError(parseEx, "Failed to parse API responses to integers for user {UserId}. Raw responses - Total: '{TotalContent}', Paid: '{PaidContent}', Unpaid: '{UnpaidContent}', Canceled: '{CanceledContent}'",
                            userId, totalContentRaw, paidContentRaw, unpaidContentRaw, canceledContentRaw);
                        return Json(new { success = false, message = "Dữ liệu không đúng định dạng" });
                    }
                }
                else
                {
                    _logger.LogWarning("One or more API calls failed for user {UserId}", userId);

                    // Log error details for failed responses
                    if (!totalResponse.IsSuccessStatusCode)
                    {
                        var errorContent = await totalResponse.Content.ReadAsStringAsync();
                        _logger.LogError("Total orders API failed for user {UserId}: {StatusCode} - {ErrorContent}",
                            userId, totalResponse.StatusCode, errorContent);
                    }

                    if (!paidResponse.IsSuccessStatusCode)
                    {
                        var errorContent = await paidResponse.Content.ReadAsStringAsync();
                        _logger.LogError("Paid orders API failed for user {UserId}: {StatusCode} - {ErrorContent}",
                            userId, paidResponse.StatusCode, errorContent);
                    }

                    if (!unpaidResponse.IsSuccessStatusCode)
                    {
                        var errorContent = await unpaidResponse.Content.ReadAsStringAsync();
                        _logger.LogError("Unpaid orders API failed for user {UserId}: {StatusCode} - {ErrorContent}",
                            userId, unpaidResponse.StatusCode, errorContent);
                    }

                    if (!canceledResponse.IsSuccessStatusCode)
                    {
                        var errorContent = await canceledResponse.Content.ReadAsStringAsync();
                        _logger.LogError("Canceled orders API failed for user {UserId}: {StatusCode} - {ErrorContent}",
                            userId, canceledResponse.StatusCode, errorContent);
                    }
                }

                return Json(new { success = false, message = "Không thể lấy dữ liệu" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order statistics for user {UserId}", userId);
                return Json(new { success = false, message = "Lỗi hệ thống" });
            }
        }
    }
}
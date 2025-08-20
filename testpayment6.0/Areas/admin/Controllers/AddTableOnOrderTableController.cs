using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using testpayment6._0.Areas.admin.Models;

namespace testpayment6._0.Areas.admin.Controllers
{
    [Area("admin")]
    public class AddTableOnOrderTableController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AddTableOnOrderTableController> _logger;
        private readonly string BASE_API_URL;

        public AddTableOnOrderTableController(HttpClient httpClient, IConfiguration configuration, ILogger<AddTableOnOrderTableController> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            BASE_API_URL = configuration["BaseAPI"] ?? "https://p7igzosmei.execute-api.ap-southeast-1.amazonaws.com/Prod/api";
        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var regions = await GetRegionsAsync();
                var viewModel = new TableOrderViewModel0508
                {
                    Regions = regions
                };
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading regions");
                ViewBag.ErrorMessage = "Không thể tải danh sách khu vực";
                return View(new TableOrderViewModel0508());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderTableDetails(int orderTableId)
        {
            try
            {
                var orderDetails = await GetOrderTableDetailsAsync(orderTableId);

                // Lấy thông tin chi tiết của từng bàn
                var detailedTables = new List<object>();
                foreach (var detail in orderDetails)
                {
                    try
                    {
                        var tableInfo = await GetTableInfoAsync(detail.TableId);
                        detailedTables.Add(new
                        {
                            orderTablesDetailsId = detail.OrderTablesDetailsId,
                            orderTableId = detail.OrderTableId,
                            tableId = detail.TableId,
                            capacity = tableInfo?.Capacity ?? 0,
                            deposit = tableInfo?.Deposit ?? 0,
                            description = tableInfo?.Description
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not get details for table {TableId}", detail.TableId);
                        // Vẫn thêm vào danh sách nhưng không có thông tin chi tiết
                        detailedTables.Add(new
                        {
                            orderTablesDetailsId = detail.OrderTablesDetailsId,
                            orderTableId = detail.OrderTableId,
                            tableId = detail.TableId,
                            capacity = 0,
                            deposit = 0,
                            description = ""
                        });
                    }
                }

                return Json(new { success = true, data = detailedTables });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order table details for order {OrderTableId}", orderTableId);
                return Json(new { success = false, message = "Không thể tải thông tin đơn đặt bàn" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTablesByRegion(int regionId)
        {
            try
            {
                var tables = await GetTablesByRegionAsync(regionId);
                return Json(new { success = true, data = tables });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading tables for region {RegionId}", regionId);
                return Json(new { success = false, message = "Không thể tải danh sách bàn" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableTablesByRegion(int regionId, int orderTableId)
        {
            try
            {
                // Lấy tất cả bàn trong khu vực
                var allTables = await GetTablesByRegionAsync(regionId);

                // Lấy danh sách bàn đã đặt trong đơn hàng
                var orderDetails = await GetOrderTableDetailsAsync(orderTableId);
                var bookedTableIds = orderDetails.Select(x => x.TableId).ToHashSet();

                // Lọc ra những bàn chưa được đặt
                var availableTables = allTables.Where(table => !bookedTableIds.Contains(table.TableId)).ToList();

                return Json(new { success = true, data = availableTables });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading available tables for region {RegionId} and order {OrderTableId}", regionId, orderTableId);
                return Json(new { success = false, message = "Không thể tải danh sách bàn có thể thêm" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddTableToOrder([FromBody] AddTableRequest request)
        {
            try
            {
                _logger.LogInformation("Received request: OrderTableId={OrderTableId}, TableId={TableId}",
                    request.OrderTableId, request.TableId);

                // Validate input
                if (request.OrderTableId <= 0)
                {
                    return Json(new { success = false, message = "Mã đơn đặt bàn không hợp lệ" });
                }

                if (request.TableId <= 0)
                {
                    return Json(new { success = false, message = "Mã bàn không hợp lệ" });
                }

                var success = await AddTableToOrderAsync(request.OrderTableId, request.TableId);

                if (success)
                {
                    return Json(new { success = true, message = "Thêm bàn thành công" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể thêm bàn vào đơn đặt bàn" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding table {TableId} to order {OrderTableId}",
                    request?.TableId, request?.OrderTableId);
                return Json(new { success = false, message = "Lỗi khi thêm bàn: " + ex.Message });
            }
        }

        // Thêm class model để bind dữ liệu
        public class AddTableRequest
        {
            public int OrderTableId { get; set; }
            public int TableId { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> AddMultipleTablesToOrder(int orderTableId, List<int> tableIds)
        {
            try
            {
                var successCount = 0;
                var errors = new List<string>();

                foreach (var tableId in tableIds)
                {
                    try
                    {
                        var success = await AddTableToOrderAsync(orderTableId, tableId);
                        if (success)
                        {
                            successCount++;
                        }
                        else
                        {
                            errors.Add($"Không thể thêm bàn {tableId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error adding table {TableId} to order {OrderTableId}", tableId, orderTableId);
                        errors.Add($"Lỗi khi thêm bàn {tableId}: {ex.Message}");
                    }
                }

                if (successCount > 0)
                {
                    var message = $"Thêm thành công {successCount}/{tableIds.Count} bàn";
                    if (errors.Any())
                    {
                        message += ". Lỗi: " + string.Join(", ", errors);
                    }
                    return Json(new { success = true, message = message });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể thêm bàn nào. Lỗi: " + string.Join(", ", errors) });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding multiple tables to order {OrderTableId}", orderTableId);
                return Json(new { success = false, message = "Lỗi khi thêm bàn: " + ex.Message });
            }
        }

        private async Task<List<RegionModel0508>> GetRegionsAsync()
        {
            var response = await _httpClient.GetAsync($"{BASE_API_URL}/region");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var regions = JsonSerializer.Deserialize<List<RegionModel0508>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return regions ?? new List<RegionModel0508>();
        }

        private async Task<List<TableModel0508>> GetTablesByRegionAsync(int regionId)
        {
            var response = await _httpClient.GetAsync($"{BASE_API_URL}/table/region/{regionId}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var tables = JsonSerializer.Deserialize<List<TableModel0508>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return tables ?? new List<TableModel0508>();
        }

        private async Task<List<OrderTableDetailModel0508>> GetOrderTableDetailsAsync(int orderTableId)
        {
            var response = await _httpClient.GetAsync($"{BASE_API_URL}/OrderTablesDetail/list/{orderTableId}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var orderDetails = JsonSerializer.Deserialize<List<OrderTableDetailModel0508>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return orderDetails ?? new List<OrderTableDetailModel0508>();
        }

        private async Task<TableModel0508?> GetTableInfoAsync(int tableId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/table/{tableId}");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var table = JsonSerializer.Deserialize<TableModel0508>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return table;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting table info for table {TableId}", tableId);
                return null;
            }
        }

        private async Task<bool> AddTableToOrderAsync(int orderTableId, int tableId)
        {
            try
            {
                // Debug log để kiểm tra dữ liệu
                _logger.LogInformation("Adding table {TableId} to order {OrderTableId}", tableId, orderTableId);

                // Thử cả 2 cách: số nguyên và string
                var requestBody = new
                {
                    OrderTableId = orderTableId,  // Thử dùng int trước
                    TableId = tableId
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                _logger.LogInformation("Request body: {RequestBody}", jsonContent);

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{BASE_API_URL}/OrderTablesDetail", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("API Error: Status {StatusCode}, Content: {ErrorContent}",
                        response.StatusCode, errorContent);

                    // Nếu lỗi, thử với format string
                    var requestBodyString = new
                    {
                        OrderTableId = orderTableId.ToString(),
                        TableId = tableId.ToString()
                    };

                    var jsonContentString = JsonSerializer.Serialize(requestBodyString);
                    var contentString = new StringContent(jsonContentString, Encoding.UTF8, "application/json");

                    var retryResponse = await _httpClient.PostAsync($"{BASE_API_URL}/OrderTablesDetail", contentString);
                    retryResponse.EnsureSuccessStatusCode();
                    return true;
                }

                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding table {TableId} to order {OrderTableId}", tableId, orderTableId);
                return false;
            }
        }
    }
}

// Bạn cũng cần các model tương ứng
namespace testpayment6._0.Areas.admin.Models
{
    public class TableOrderViewModel0508
    {
        public List<RegionModel0508> Regions { get; set; } = new List<RegionModel0508>();
    }

    public class RegionModel0508
    {
        public int RegionId { get; set; }
        public string RegionName { get; set; } = string.Empty;
    }

    public class TableModel0508
    {
        public int TableId { get; set; }
        public int Capacity { get; set; }
        public decimal Deposit { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class OrderTableDetailModel0508
    {
        public int OrderTablesDetailsId { get; set; }
        public int OrderTableId { get; set; }
        public int TableId { get; set; }
    }
}
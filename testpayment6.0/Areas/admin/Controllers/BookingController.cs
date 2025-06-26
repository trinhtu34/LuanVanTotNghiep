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
        private string BASE_API_URL ;

        public BookingController(HttpClient httpClient, ILogger<BookingController> logger , IConfiguration configuration)
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

            // QUAN TRỌNG: Khởi tạo SelectedDishes với đúng số lượng items
            if (model.Dishes != null && model.Dishes.Any())
            {
                model.SelectedDishes = new List<SelectedDish>();
                foreach (var dish in model.Dishes)
                {
                    model.SelectedDishes.Add(new SelectedDish
                    {
                        DishId = dish.dishId,
                        DishName = dish.dishName,
                        Price = dish.price,
                        Quantity = 0
                    });
                }
            }

            _logger.LogInformation("Initialized SelectedDishes with {Count} items", model.SelectedDishes?.Count ?? 0);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBooking(BookingViewModel model)
        {
            // Filter selected dishes (only those with quantity > 0)
            var selectedDishes = model.SelectedDishes?.Where(d => d != null && d.Quantity > 0).ToList();
            _logger.LogInformation("Filtered dishes with quantity > 0: {Count}", selectedDishes?.Count ?? 0);

            if (selectedDishes == null || !selectedDishes.Any())
            {
                _logger.LogWarning("No dishes selected");
                TempData["Error"] = "Vui lòng chọn ít nhất một món ăn!";
                var reloadedModel = await LoadBookingData();
                // Preserve form data
                reloadedModel.CustomerName = model.CustomerName;
                reloadedModel.PhoneNumber = model.PhoneNumber;
                reloadedModel.SelectedTableId = model.SelectedTableId;
                reloadedModel.SelectedDishes = model.SelectedDishes;
                return View(reloadedModel);
            }

            try
            {
                _logger.LogInformation("Starting booking process...");

                // 1. Tạo tài khoản cho khách hàng
                //var userId = DateTime.Now.Ticks % 1000000000000000;
                //var userIdString = userId.ToString("D15"); // Đảm bảo ID có độ dài 15 ký tự

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

                var userInfo = JsonSerializer.Deserialize<userResponse>(signupResponseContent);
                string useridString = userInfo.userId;
                // 2. Tạo đơn đặt bàn
                var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                var orderRequest = new OrderTableRequest
                {
                    userId = useridString,
                    isCancel = false,
                    startingTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone)
                };

                var orderJson = JsonSerializer.Serialize(orderRequest);
                var orderContent = new StringContent(orderJson, Encoding.UTF8, "application/json");
                var orderResponse = await _httpClient.PostAsync($"{BASE_API_URL}/OrderTable", orderContent);

                var orderResponseContent = await orderResponse.Content.ReadAsStringAsync();
                _logger.LogInformation("Order table response: {Response}", orderResponseContent);

                // đến đây là ok rồi , nhận thông tin đưa về chuẩn rồi , mà mấy cái giá tiền bị null , trong khi chọn món ăn rồi , chịu luôn

                var orderTableResponse = JsonSerializer.Deserialize<OrderTableResponse>(orderResponseContent);
                if (orderTableResponse == null)
                {
                    _logger.LogError("Failed to deserialize order table response");
                    TempData["Error"] = "Không thể đọc dữ liệu từ đơn đặt bàn";
                    return RedirectToAction("CreateBooking");
                }

                var orderTableId = orderTableResponse.orderTableId;
                _logger.LogInformation("Order table created with ID: {OrderTableId}", orderTableId);

                // 3. Thêm bàn vào đơn đặt bàn
                _logger.LogInformation("Adding table {TableId} to order...", model.SelectedTableId);
                var tableDetailRequest = new OrderTableDetailRequest
                {
                    orderTableId = orderTableId,
                    TableId = model.SelectedTableId.ToString()
                };

                var tableDetailJson = JsonSerializer.Serialize(tableDetailRequest);
                var tableDetailContent = new StringContent(tableDetailJson, Encoding.UTF8, "application/json");
                var tableDetailResponse = await _httpClient.PostAsync($"{BASE_API_URL}/OrderTablesDetail", tableDetailContent);

                if (!tableDetailResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to add table to order. Status: {Status}", tableDetailResponse.StatusCode);
                    TempData["Error"] = "Không thể thêm bàn vào đơn đặt bàn";
                    return RedirectToAction("CreateBooking");
                }

                // 4. Thêm món ăn vào đơn đặt bàn
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
                        _logger.LogWarning("Failed to add dish {DishId} to order. Status: {Status}",
                            dish.DishId, foodResponse.StatusCode);
                    }
                    else
                    {
                        _logger.LogInformation("Successfully added dish {DishId}", dish.DishId);
                    }
                }

                TempData["Success"] = $"Đặt bàn thành công cho khách hàng {model.CustomerName}!";
                return RedirectToAction("CreateBooking");
            }
            catch (Exception ex)
            {
                return RedirectToAction("CreateBooking");
            }
        }

        private async Task<BookingViewModel> LoadBookingData()
        {
            var model = new BookingViewModel();

            try
            {
                _logger.LogInformation("Loading booking data...");

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
                ViewBag.Error = "Không thể tải dữ liệu: " + ex.Message;
            }

            return model;
        }
    }
}
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
            try
            {
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

                // validate thời gian đăng ký , cho chọn luôn , không dùng datetime nữa , vì có thể khách đến trực tiếp đặt cho thời gian sau

                // Lấy múi giờ Việt Nam
                var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                if (!DateTime.TryParse(model.startingTime.ToString(), out DateTime parsedStartTime))
                {
                    return Json(new { success = false, message = "Thời gian không hợp lệ" });
                }
                DateTime nowInVietnam = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);
                if (parsedStartTime <= nowInVietnam)
                {
                    return Json(new { success = false, message = "Thời gian phải trong tương lai" });
                }

                var userInfo = JsonSerializer.Deserialize<userResponse>(signupResponseContent);
                string useridString = userInfo.userId;
                // 2. Tạo đơn đặt bàn
                var orderRequest = new OrderTableRequest
                {
                    userId = useridString,
                    isCancel = false,
                    //startingTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone)
                    startingTime = parsedStartTime
                };

                var orderJson = JsonSerializer.Serialize(orderRequest);
                var orderContent = new StringContent(orderJson, Encoding.UTF8, "application/json");
                var orderResponse = await _httpClient.PostAsync($"{BASE_API_URL}/OrderTable", orderContent);

                var orderResponseContent = await orderResponse.Content.ReadAsStringAsync();
                _logger.LogInformation("Order table response: {Response}", orderResponseContent);

                var orderTableResponse = JsonSerializer.Deserialize<OrderTableResponse>(orderResponseContent);
                var orderTableId = orderTableResponse.orderTableId;

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
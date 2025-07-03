using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using testpayment6._0.Models;
using VNPAY.NET;
using VNPAY.NET.Enums;
using VNPAY.NET.Models;

namespace VnPayDemo.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IVnpay _vnpay;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentController> _logger;
        private readonly HttpClient _httpClient;
        private readonly string BASE_API_URL;

        public PaymentController(IVnpay vnpay, IConfiguration configuration, ILogger<PaymentController> logger, HttpClient httpClient)
        {
            _vnpay = vnpay;
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
            BASE_API_URL = configuration["BaseAPI"];

            _vnpay.Initialize(
                _configuration["VnPay:TmnCode"],
                _configuration["VnPay:HashSecret"],
                _configuration["VnPay:BaseUrl"],
                _configuration["VnPay:ReturnUrl"]
            );
        }

        [HttpPost]
        public IActionResult Index(PaymentViewModel model)
        {
            _logger.LogInformation($"Payment Index - Amount: {model.Amount}, Description: {model.Description}, OrderTableId: {model.OrderTableId}, CartId: {model.CartId}");

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogWarning($"Validation error: {error.ErrorMessage}");
                }
                return View(model);
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePayment(PaymentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            try
            {
                if (model.Amount < 10000 || model.Amount > 100000000)
                {
                    ModelState.AddModelError("Amount", "Số tiền phải từ 10.000đ đến 100.000.000đ");
                    return View("Index", model);
                }

                string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

                // Xử lý PaymentId dựa trên Cart hoặc OrderTable
                long paymentId;
                var timestamp = DateTime.Now.Ticks % 1000000000;

                if (model.CartId.HasValue && model.CartId.Value > 0)
                {
                    // Encode CartId vào PaymentId (thêm prefix để phân biệt)
                    paymentId = long.Parse($"2{timestamp}{model.CartId.Value:D8}"); // prefix "2" cho Cart

                    // Lưu vào session
                    HttpContext.Session.SetString("CartId", model.CartId.Value.ToString());
                    HttpContext.Session.SetString($"CartId_{paymentId}", model.CartId.Value.ToString());
                }
                else if (model.OrderTableId.HasValue && model.OrderTableId.Value > 0)
                {
                    // Encode OrderTableId vào PaymentId (giữ nguyên logic cũ)
                    paymentId = long.Parse($"1{timestamp}{model.OrderTableId.Value:D8}"); // prefix "1" cho OrderTable

                    // Lưu vào session
                    HttpContext.Session.SetString("OrderTableId", model.OrderTableId.Value.ToString());
                    HttpContext.Session.SetString($"OrderTableId_{paymentId}", model.OrderTableId.Value.ToString());
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Không tìm thấy thông tin đơn hàng");
                    return View("Index", model);
                }

                var request = new PaymentRequest
                {
                    PaymentId = paymentId,
                    Money = (long)model.Amount,
                    Description = model.Description,
                    IpAddress = ipAddress,
                    BankCode = BankCode.ANY,
                    CreatedDate = DateTime.Now,
                    Currency = Currency.VND,
                    Language = DisplayLanguage.Vietnamese
                };

                await HttpContext.Session.CommitAsync();

                // Log để debug
                _logger.LogInformation($"Created PaymentId: {paymentId} for CartId: {model.CartId}, OrderTableId: {model.OrderTableId}");

                var paymentUrl = _vnpay.GetPaymentUrl(request);
                _logger.LogInformation($"Payment URL: {paymentUrl}");

                return Redirect(paymentUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo URL thanh toán");
                ModelState.AddModelError(string.Empty, $"Có lỗi xảy ra: {ex.Message}");
                return View("Index", model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Callback()
        {
            _logger.LogInformation($"Session ID: {HttpContext.Session.Id}");
            _logger.LogInformation($"Available session keys: {string.Join(", ", HttpContext.Session.Keys)}");

            if (Request.QueryString.HasValue)
            {
                try
                {
                    var paymentResult = _vnpay.GetPaymentResult(Request.Query);
                    long orderTableId = 0;
                    long cartId = 0;
                    decimal amount = 0;

                    var vnp_AmountStr = Request.Query["vnp_Amount"];
                    if (!string.IsNullOrEmpty(vnp_AmountStr))
                    {
                        if (long.TryParse(vnp_AmountStr, out var vnpAmount))
                        {
                            amount = vnpAmount / 100m;
                        }
                    }

                    // Decode ID từ PaymentId
                    var paymentIdStr = paymentResult.PaymentId.ToString();
                    if (paymentIdStr.Length >= 10) // Ít nhất 1 (prefix) + 9 (timestamp portion) + 8 (id)
                    {
                        var prefix = paymentIdStr.Substring(0, 1);
                        var idPart = paymentIdStr.Substring(paymentIdStr.Length - 8); // 8 số cuối

                        if (prefix == "2") // Cart
                        {
                            if (long.TryParse(idPart, out var decodedCartId) && decodedCartId > 0)
                            {
                                cartId = decodedCartId;
                            }
                        }
                        else if (prefix == "1") // OrderTable
                        {
                            if (long.TryParse(idPart, out var decodedOrderTableId) && decodedOrderTableId > 0)
                            {
                                orderTableId = decodedOrderTableId;
                            }
                        }
                    }

                    // Fallback: Thử lấy từ session
                    if (cartId == 0 && orderTableId == 0)
                    {
                        var cartIdStr = HttpContext.Session.GetString("CartId");
                        if (!string.IsNullOrEmpty(cartIdStr) && long.TryParse(cartIdStr, out var sessionCartId))
                        {
                            cartId = sessionCartId;
                        }
                        else
                        {
                            var orderTableIdStr = HttpContext.Session.GetString("OrderTableId");
                            if (!string.IsNullOrEmpty(orderTableIdStr) && long.TryParse(orderTableIdStr, out var sessionOrderTableId))
                            {
                                orderTableId = sessionOrderTableId;
                            }
                        }
                    }

                    var resultModel = new PaymentResultViewModel
                    {
                        PaymentId = paymentResult.PaymentId,
                        Amount = amount,
                        IsSuccess = paymentResult.IsSuccess,
                        Description = paymentResult.Description,
                        Timestamp = paymentResult.Timestamp,
                        VnpayTransactionId = paymentResult.VnpayTransactionId,
                        PaymentMethod = paymentResult.PaymentMethod,
                        BankCode = paymentResult.BankingInfor?.BankCode ?? string.Empty,
                        BankTransactionId = paymentResult.BankingInfor?.BankTransactionId ?? string.Empty,
                        ResponseDescription = paymentResult.PaymentResponse?.Description ?? string.Empty,
                        TransactionStatusDescription = paymentResult.TransactionStatus?.Description ?? string.Empty,
                        OrderTableId = orderTableId,
                        CartId = cartId
                    };

                    _logger.LogInformation($"Parsed amount: {amount} from vnp_Amount: {vnp_AmountStr}");
                    _logger.LogInformation($"Decoded CartId: {cartId}, OrderTableId: {orderTableId}");

                    var saveResult = await SavePaymentToDatabase(resultModel);
                    if (paymentResult.IsSuccess)
                    {
                        //Xóa session cart nếu thanh toán thành công và là Cart
                        if (cartId > 0)
                        {
                            HttpContext.Session.Remove("Cart");
                        }

                        return View("Success", resultModel);
                    }
                    else
                    {
                        return View("Error", resultModel);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi xử lý kết quả thanh toán");
                    return View("Error", new PaymentResultViewModel
                    {
                        IsSuccess = false,
                        Description = $"Lỗi xử lý kết quả thanh toán: {ex.Message}"
                    });
                }
            }

            return View("Error", new PaymentResultViewModel
            {
                IsSuccess = false,
                Description = "Không tìm thấy thông tin thanh toán"
            });
        }

        private async Task<SavePaymentResult> SavePaymentToDatabase(PaymentResultViewModel paymentResult)
        {
            try
            {
                // Xác định giá trị null dựa trên logic business
                long? orderTableId = null;
                long? cartId = null;

                if (paymentResult.CartId > 0)
                {
                    cartId = paymentResult.CartId;
                    orderTableId = null; // Đảm bảo orderTableId là null khi có cartId
                }
                else if (paymentResult.OrderTableId > 0)
                {
                    orderTableId = paymentResult.OrderTableId;
                    cartId = null; // Đảm bảo cartId là null khi có orderTableId
                }
                _logger.LogInformation($"test order table id: {paymentResult.OrderTableId}");
                // Tạo payload để gửi đến API
                var paymentData = new
                {
                    orderTableId = orderTableId, // Sử dụng biến đã xử lý
                    cartId = cartId, // Sử dụng biến đã xử lý
                    amount = paymentResult.Amount,
                    paymentId = paymentResult.PaymentId,
                    isSuccess = paymentResult.IsSuccess,
                    description = paymentResult.Description,
                    timestamp = paymentResult.Timestamp.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    vnpayTransactionId = paymentResult.VnpayTransactionId,
                    paymentMethod = paymentResult.PaymentMethod ?? "VNPAY",
                    bankCode = paymentResult.BankCode ?? string.Empty,
                    bankTransactionId = paymentResult.BankTransactionId ?? string.Empty,
                    responseDescription = paymentResult.ResponseDescription ?? string.Empty,
                    transactionStatusDescription = paymentResult.TransactionStatusDescription ?? string.Empty
                };

                var jsonContent = JsonSerializer.Serialize(paymentData, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                });

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation($"Gửi dữ liệu thanh toán đến API: {jsonContent}");

                var response = await _httpClient.PostAsync($"{BASE_API_URL}/payment", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Lưu dữ liệu thanh toán thành công. Response: {responseContent}");

                    return new SavePaymentResult
                    {
                        IsSuccess = true,
                        ResponseContent = responseContent
                    };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorMessage = $"API trả về lỗi: {response.StatusCode} - {errorContent}";
                    _logger.LogError(errorMessage);

                    return new SavePaymentResult
                    {
                        IsSuccess = false,
                        ErrorMessage = errorMessage
                    };
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Lỗi không xác định khi lưu dữ liệu thanh toán: {ex.Message}";
                _logger.LogError(ex, errorMessage);

                return new SavePaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = errorMessage
                };
            }
        }
    }

        public class SavePaymentResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ResponseContent { get; set; }
    }
}
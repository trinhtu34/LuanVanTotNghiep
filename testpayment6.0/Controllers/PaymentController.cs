using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Net;
using VNPAY.NET;
using testpayment6._0.Models;
using VNPAY.NET.Enums;
using VNPAY.NET.Models;

namespace VnPayDemo.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IVnpay _vnpay;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IVnpay vnpay, IConfiguration configuration, ILogger<PaymentController> logger)
        {
            _vnpay = vnpay;
            _configuration = configuration;
            _logger = logger;

            // Khởi tạo VNPAY từ cấu hình
            _vnpay.Initialize(
                _configuration["VnPay:TmnCode"],
                _configuration["VnPay:HashSecret"],
                _configuration["VnPay:BaseUrl"],
                _configuration["VnPay:ReturnUrl"]
            );
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CreatePayment(PaymentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            try
            {
                // Lấy địa chỉ IP của thiết bị thực hiện giao dịch
                string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

                // Tạo yêu cầu thanh toán
                var request = new PaymentRequest
                {
                    PaymentId = DateTime.Now.Ticks, // Tạo ID giao dịch duy nhất
                    Money = model.Amount,
                    Description = model.Description,
                    IpAddress = ipAddress,
                    BankCode = BankCode.ANY, // Cho phép chọn mọi ngân hàng
                    CreatedDate = DateTime.Now,
                    Currency = Currency.VND,
                    Language = DisplayLanguage.Vietnamese
                };

                // Lấy URL thanh toán
                var paymentUrl = _vnpay.GetPaymentUrl(request);

                // Chuyển hướng người dùng đến trang thanh toán của VNPAY
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
        public IActionResult Callback()
        {
            if (Request.QueryString.HasValue)
            {
                try
                {
                    // Xử lý kết quả trả về từ VNPAY
                    var paymentResult = _vnpay.GetPaymentResult(Request.Query);

                    // Tạo model để hiển thị kết quả
                    var resultModel = new PaymentResultViewModel
                    {
                        PaymentId = paymentResult.PaymentId,
                        IsSuccess = paymentResult.IsSuccess,
                        Description = paymentResult.Description,
                        Timestamp = paymentResult.Timestamp,
                        VnpayTransactionId = paymentResult.VnpayTransactionId,
                        PaymentMethod = paymentResult.PaymentMethod,
                        BankCode = paymentResult.BankingInfor?.BankCode ?? string.Empty,
                        BankTransactionId = paymentResult.BankingInfor?.BankTransactionId ?? string.Empty,
                        ResponseDescription = paymentResult.PaymentResponse?.Description ?? string.Empty,
                        TransactionStatusDescription = paymentResult.TransactionStatus?.Description ?? string.Empty
                    };

                    // Chuyển hướng đến trang thành công hoặc lỗi tùy thuộc vào kết quả
                    if (paymentResult.IsSuccess)
                    {
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
    }
}
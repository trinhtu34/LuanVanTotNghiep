using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using testpayment6._0.Models;
using testpayment6._0.ResponseModels;

namespace testpayment6._0.Controllers
{
    public class ContactController : Controller
    {
        private readonly ILogger<ContactController> _logger;
        private readonly HttpClient _httpClient;
        private readonly string BASE_API_URL;

        public ContactController(ILogger<ContactController> logger, HttpClient httpClient, IConfiguration configuration)
        {
            _logger = logger;
            _httpClient = httpClient;
            BASE_API_URL = configuration["BaseAPI"];
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login", "Home");
            }

            var userId = HttpContext.Session.GetString("UserId");
            var model = new ContactListViewModel();

            try
            {
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/contactform/{userId}");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var contacts = JsonSerializer.Deserialize<List<ContactResponse>>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    model.Contacts = contacts?.Select(c => new ContactItem
                    {
                        ContactId = c.ContactId,
                        UserId = c.UserId,
                        Content = c.Content,
                        CreateAt = c.CreateAt
                    }).OrderByDescending(c => c.CreateAt).ToList() ?? new List<ContactItem>();

                    _logger.LogInformation($"Retrieved {model.Contacts.Count} contacts for user {userId}");
                }
                else
                {
                    _logger.LogWarning($"Failed to get contacts for user {userId}: {response.StatusCode}");
                    ViewBag.ErrorContact = "Bạn chưa có liên hệ nào với nhà hàng .";
                }
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, $"Network error getting contacts for user {userId}");
                ViewBag.ErrorContact = "Không thể kết nối đến server. Vui lòng thử lại.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error getting contacts for user {userId}");
                ViewBag.ErrorContact = "Đã xảy ra lỗi. Vui lòng thử lại.";
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ContactListViewModel model)
        {
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login", "Home");
            }

            var userId = HttpContext.Session.GetString("UserId");

            if (!ModelState.IsValid)
            {
                await LoadContactsForModel(model, userId);
                return View("Index", model);
            }

            try
            {
                var request = new
                {
                    UserId = userId,
                    Content = model.NewContact.Content.Trim()
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation($"Creating contact for user: {userId}");
                var response = await _httpClient.PostAsync($"{BASE_API_URL}/ContactForm", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Contact created successfully for user {userId}");

                    TempData["MessageContact"] = "Gửi liên hệ thành công! Chúng tôi sẽ phản hồi sớm nhất có thể.";
                    return RedirectToAction("Index");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Contact creation failed for user {userId}: {response.StatusCode} - {errorContent}");

                    ViewBag.ErrorContact = "Gửi liên hệ không thành công. Vui lòng thử lại.";
                }
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, $"Network error during contact creation for user {userId}");
                ViewBag.ErrorContact = "Không thể kết nối đến server. Vui lòng thử lại.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error during contact creation for user {userId}");
                ViewBag.ErrorContact = "Đã xảy ra lỗi. Vui lòng thử lại.";
            }

            await LoadContactsForModel(model, userId);
            return View("Index", model);
        }

        private async Task LoadContactsForModel(ContactListViewModel model, string userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"https://p7igzosmei.execute-api.ap-southeast-1.amazonaws.com/Prod/api/contactform/{userId}");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var contacts = JsonSerializer.Deserialize<List<ContactResponse>>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    model.Contacts = contacts?.Select(c => new ContactItem
                    {
                        ContactId = c.ContactId,
                        UserId = c.UserId,
                        Content = c.Content,
                        CreateAt = c.CreateAt
                    }).OrderByDescending(c => c.CreateAt).ToList() ?? new List<ContactItem>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading contacts for user {userId}");
                model.Contacts = new List<ContactItem>();
            }
        }

        private bool IsUserLoggedIn()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var isAuthenticated = HttpContext.Session.GetString("IsAuthenticated");

            return !string.IsNullOrEmpty(userId) && isAuthenticated == "true";
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Globalization;
using testpayment6._0.ResponseModels;

namespace testpayment6._0.Areas.admin.Controllers
{
    [Area("admin")]
    public class View_ContactController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string BASE_API_URL;

        public View_ContactController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            BASE_API_URL = configuration["BaseAPI"];
        }

        public async Task<IActionResult> Index(string filterType = "all", string startDate = "", string endDate = "")
        {
            // Kiểm tra session
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("IndexAdminLogin", "HomeAdmin");
            }

            try
            {
                using var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync("https://p7igzosmei.execute-api.ap-southeast-1.amazonaws.com/Prod/api/contactform");

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var contacts = JsonConvert.DeserializeObject<List<ContactModel>>(responseString);

                    // Áp dụng filter
                    var filteredContacts = ApplyFilter(contacts, filterType, startDate, endDate);

                    ViewBag.FilterType = filterType;
                    ViewBag.StartDate = startDate;
                    ViewBag.EndDate = endDate;
                    ViewBag.TotalContacts = filteredContacts.Count;
                    ViewBag.UserId = userId;

                    return View(filteredContacts);
                }
                else
                {
                    ViewBag.Error = "Không thể tải dữ liệu liên hệ.";
                    return View(new List<ContactModel>());
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Lỗi: {ex.Message}";
                return View(new List<ContactModel>());
            }
        }

        private List<ContactModel> ApplyFilter(List<ContactModel> contacts, string filterType, string startDate, string endDate)
        {
            var now = DateTime.Now;

            switch (filterType.ToLower())
            {
                case "today":
                    return contacts.Where(c => c.CreateAt.Date == now.Date).ToList();

                case "yesterday":
                    return contacts.Where(c => c.CreateAt.Date == now.Date.AddDays(-1)).ToList();

                case "thisweek":
                    var startOfWeek = now.Date.AddDays(-(int)now.DayOfWeek);
                    var endOfWeek = startOfWeek.AddDays(6);
                    return contacts.Where(c => c.CreateAt.Date >= startOfWeek && c.CreateAt.Date <= endOfWeek).ToList();

                case "lastweek":
                    var startOfLastWeek = now.Date.AddDays(-(int)now.DayOfWeek - 7);
                    var endOfLastWeek = startOfLastWeek.AddDays(6);
                    return contacts.Where(c => c.CreateAt.Date >= startOfLastWeek && c.CreateAt.Date <= endOfLastWeek).ToList();

                case "thismonth":
                    var startOfMonth = new DateTime(now.Year, now.Month, 1);
                    var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
                    return contacts.Where(c => c.CreateAt.Date >= startOfMonth && c.CreateAt.Date <= endOfMonth).ToList();

                case "lastmonth":
                    var startOfLastMonth = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
                    var endOfLastMonth = startOfLastMonth.AddMonths(1).AddDays(-1);
                    return contacts.Where(c => c.CreateAt.Date >= startOfLastMonth && c.CreateAt.Date <= endOfLastMonth).ToList();

                case "custom":
                    if (DateTime.TryParseExact(startDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start) &&
                        DateTime.TryParseExact(endDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
                    {
                        return contacts.Where(c => c.CreateAt.Date >= start && c.CreateAt.Date <= end).ToList();
                    }
                    return contacts;

                default:
                    return contacts;
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteContact(int contactId)
        {
            try
            {
                using var client = _httpClientFactory.CreateClient();
                var response = await client.DeleteAsync($"https://p7igzosmei.execute-api.ap-southeast-1.amazonaws.com/Prod/api/contactform/{contactId}");

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Xóa liên hệ thành công!";
                }
                else
                {
                    TempData["Error"] = "Không thể xóa liên hệ.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> GetContactDetails(int contactId)
        {
            try
            {
                using var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync($"{BASE_API_URL}/contactform/{contactId}");

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var contact = JsonConvert.DeserializeObject<ContactModel>(responseString);
                    return Json(contact);
                }
                else
                {
                    return Json(new { error = "Không thể tải chi tiết liên hệ." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = $"Lỗi: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportContacts(string filterType = "all", string startDate = "", string endDate = "")
        {
            try
            {
                using var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync($"{BASE_API_URL}/contactform");

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var contacts = JsonConvert.DeserializeObject<List<ContactModel>>(responseString);
                    var filteredContacts = ApplyFilter(contacts, filterType, startDate, endDate);

                    // Tạo CSV content
                    var csvContent = "Contact ID,User ID,Content,Create Date\n";
                    foreach (var contact in filteredContacts.OrderByDescending(c => c.CreateAt))
                    {
                        csvContent += $"{contact.ContactId},\"{contact.UserId}\",\"{contact.Content.Replace("\"", "\"\"")}\",{contact.CreateAt:yyyy-MM-dd HH:mm:ss}\n";
                    }

                    var bytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
                    return File(bytes, "text/csv", $"contacts_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                }
                else
                {
                    TempData["Error"] = "Không thể xuất dữ liệu liên hệ.";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi xuất file: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
    }

    // Model cho Contact
    public class ContactModel
    {
        public int ContactId { get; set; }
        public string UserId { get; set; }
        public string Content { get; set; }
        public DateTime CreateAt { get; set; }
    }
}
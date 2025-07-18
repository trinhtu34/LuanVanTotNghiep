using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using testpayment6._0.Areas.admin.Models;

namespace testpayment6._0.Areas.admin.Controllers
{
    [Area("admin")]
    public class ContactManagementController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string BASE_API_URL;

        public ContactManagementController(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            BASE_API_URL = configuration["BaseAPI"];
        }

        public async Task<IActionResult> Index(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string userId = null,
            string contentSearch = null,
            string searchType = "contains",
            bool caseSensitive = false,
            string sortBy = "createAt",
            string sortOrder = "desc",
            int pageNumber = 1,
            int pageSize = 50,
            string quickDateFilter = null)
        {
            var viewModel = new ContactFormViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                UserId = userId,
                ContentSearch = contentSearch,
                SearchType = searchType,
                CaseSensitive = caseSensitive,
                SortBy = sortBy,
                SortOrder = sortOrder,
                PageNumber = pageNumber,
                PageSize = pageSize,
                QuickDateFilter = quickDateFilter
            };

            // Xử lý quick date filter
            if (!string.IsNullOrEmpty(quickDateFilter))
            {
                var dates = ProcessQuickDateFilter(quickDateFilter);
                viewModel.FromDate = dates.fromDate;
                viewModel.ToDate = dates.toDate;
            }

            try
            {
                var result = await GetContactFormsWithFiltersAsync(
                    viewModel.FromDate,
                    viewModel.ToDate,
                    viewModel.UserId,
                    viewModel.ContentSearch,
                    viewModel.SearchType,
                    viewModel.CaseSensitive,
                    viewModel.SortBy,
                    viewModel.SortOrder,
                    viewModel.PageNumber,
                    viewModel.PageSize);

                viewModel.ContactForms = result.data;
                viewModel.TotalRecords = result.totalRecords;
                viewModel.TotalPages = result.totalPages;
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessageContact = $"Có lỗi xảy ra: {ex.Message}";
            }

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetContactDetail(int ContactId)
        {
            try
            {
                var contact = await GetContactByIdAsync(ContactId);
                if (contact == null)
                {
                    return NotFound();
                }
                return PartialView("_ContactDetailModal", contact);
            }
            catch (Exception ex)
            {
                return BadRequest($"Có lỗi xảy ra: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportToExcel(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string userId = null,
            string contentSearch = null,
            string searchType = "contains",
            bool caseSensitive = false,
            string sortBy = "createAt",
            string sortOrder = "desc")
        {
            try
            {
                var result = await GetContactFormsWithFiltersAsync(
                    fromDate, toDate, userId, contentSearch, searchType,
                    caseSensitive, sortBy, sortOrder, 1, int.MaxValue);

                var csvContent = GenerateCSV(result.data);
                var bytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
                var fileName = $"contact_forms_{DateTime.Now:yyyy-MM-dd}.csv";

                return File(bytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessageContact"] = $"Có lỗi xảy ra khi xuất dữ liệu: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        private async Task<(List<ContactFormModel> data, int totalRecords, int totalPages)> GetContactFormsWithFiltersAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string userId = null,
            string contentSearch = null,
            string searchType = "contains",
            bool caseSensitive = false,
            string sortBy = "createAt",
            string sortOrder = "desc",
            int pageNumber = 1,
            int pageSize = 50)
        {
            try
            {
                var queryParams = new List<string>();

                if (fromDate.HasValue)
                    queryParams.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
                if (toDate.HasValue)
                    queryParams.Add($"toDate={toDate.Value:yyyy-MM-dd}");
                if (!string.IsNullOrEmpty(userId))
                    queryParams.Add($"userId={Uri.EscapeDataString(userId)}");
                if (!string.IsNullOrEmpty(contentSearch))
                    queryParams.Add($"contentSearch={Uri.EscapeDataString(contentSearch)}");
                if (!string.IsNullOrEmpty(searchType))
                    queryParams.Add($"searchType={searchType}");
                if (caseSensitive)
                    queryParams.Add("caseSensitive=true");
                if (!string.IsNullOrEmpty(sortBy))
                    queryParams.Add($"sortBy={sortBy}");
                if (!string.IsNullOrEmpty(sortOrder))
                    queryParams.Add($"sortOrder={sortOrder}");

                queryParams.Add($"pageNumber={pageNumber}");
                queryParams.Add($"pageSize={pageSize}");

                var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/contactform/filter/sub{queryString}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<ContactFormModel>>(jsonContent) ?? new List<ContactFormModel>();

                    // Lấy thông tin pagination từ headers
                    var totalRecords = 0;
                    var totalPages = 1;

                    if (response.Headers.TryGetValues("X-Total-Count", out var totalCountValues))
                        int.TryParse(totalCountValues.FirstOrDefault(), out totalRecords);

                    if (response.Headers.TryGetValues("X-Total-Pages", out var totalPagesValues))
                        int.TryParse(totalPagesValues.FirstOrDefault(), out totalPages);

                    return (data, totalRecords, totalPages);
                }
                else
                {
                    throw new Exception("Không thể tải dữ liệu liên hệ");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Có lỗi xảy ra: {ex.Message}");
            }
        }
        // nhớ là nó trả về array nên phải deserialize thành List trước rồi mới lấy item đầu tiên ( thật ra là chỉ có 1 item thôi ), đây là api lấy chỉ định ID nên chỉ có 1 thôi, gặp vấn đề này lần thứ n rồi phải nhớ !
        private async Task<ContactFormModel> GetContactByIdAsync(int contactId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BASE_API_URL}/contactform/contactid/{contactId}");
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();

                    var contactList = JsonConvert.DeserializeObject<List<ContactFormModel>>(jsonContent);

                    // trả về item đầu tiên ( thật ra chỉ có 1 thôi ) , hoặc trả về null nếu không có dữ liệu
                    return contactList?.FirstOrDefault();
                }
                else
                {
                    throw new Exception("Không thể tải thông tin liên hệ");
                }
            }
            catch (JsonSerializationException ex)
            {
                throw new Exception($"Lỗi parse JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Có lỗi xảy ra: {ex.Message}");
            }
        }
        private (DateTime? fromDate, DateTime? toDate) ProcessQuickDateFilter(string quickDateFilter)
        {
            var today = DateTime.Today;
            DateTime? fromDate = null;
            DateTime? toDate = null;

            switch (quickDateFilter)
            {
                case "today":
                    fromDate = toDate = today;
                    break;
                case "yesterday":
                    fromDate = toDate = today.AddDays(-1);
                    break;
                case "thisWeek":
                    var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                    fromDate = startOfWeek;
                    toDate = today;
                    break;
                case "lastWeek":
                    var lastWeekStart = today.AddDays(-(int)today.DayOfWeek - 7);
                    var lastWeekEnd = today.AddDays(-(int)today.DayOfWeek - 1);
                    fromDate = lastWeekStart;
                    toDate = lastWeekEnd;
                    break;
                case "thisMonth":
                    fromDate = new DateTime(today.Year, today.Month, 1);
                    toDate = today;
                    break;
                case "lastMonth":
                    fromDate = new DateTime(today.Year, today.Month - 1, 1);
                    toDate = new DateTime(today.Year, today.Month, 1).AddDays(-1);
                    break;
                case "thisYear":
                    fromDate = new DateTime(today.Year, 1, 1);
                    toDate = today;
                    break;
                case "lastYear":
                    fromDate = new DateTime(today.Year - 1, 1, 1);
                    toDate = new DateTime(today.Year - 1, 12, 31);
                    break;
            }

            return (fromDate, toDate);
        }

        private string GenerateCSV(List<ContactFormModel> data)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Contact ID,User ID,Nội dung,Thời gian tạo");

            foreach (var contact in data)
            {
                csv.AppendLine($"{contact.ContactId},{contact.UserId},\"{contact.Content.Replace("\"", "\"\"")}\",{contact.CreateAt:dd/MM/yyyy HH:mm:ss}");
            }

            return csv.ToString();
        }
    }
}
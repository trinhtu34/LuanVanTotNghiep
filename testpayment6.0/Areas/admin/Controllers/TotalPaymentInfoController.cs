using Microsoft.AspNetCore.Mvc;

namespace testpayment6._0.Areas.admin.Controllers
{
    [Area("admin")]
    public class TotalPaymentInfoController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string BASE_API_URL;
        public TotalPaymentInfoController(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            BASE_API_URL = configuration["BaseAPI"];
        }


        public IActionResult Index()
        {
            return View();
        }
    }
}

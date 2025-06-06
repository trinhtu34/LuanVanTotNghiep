using Microsoft.AspNetCore.Mvc;

namespace testpayment6._0.Areas.admin.Controllers
{
    [Area("admin")]
    public class CategoryController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using testpayment6._0.Models;

namespace VnPayDemo.Controllers
{
    public class ProductController : Controller
    {
        public IActionResult Index()
        {
            // Danh sách sản phẩm mẫu
            var products = new List<Product>
            {
                new Product { Id = 1, Name = "Áo Thun", Price = 150000, Description = "Áo thun cotton thoáng mát" },
                new Product { Id = 2, Name = "Quần Jean", Price = 350000, Description = "Quần jean nam phong cách" },
                new Product { Id = 3, Name = "Giày Sneaker", Price = 900000, Description = "Giày sneaker năng động" }
            };

            return View(products);
        }

        // Chuyển đến trang thanh toán với thông tin sản phẩm
        public IActionResult Order(int id, string name, double price)
        {
            var model = new PaymentViewModel
            {
                Amount = price,
                Description = $"Thanh toán sản phẩm: {name}"
            };

            return View("~/Views/Payment/Index.cshtml", model);
        }
    }
}

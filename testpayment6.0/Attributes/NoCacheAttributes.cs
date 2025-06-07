using Microsoft.AspNetCore.Mvc.Filters;

namespace testpayment6._0.Attributes
{
    public class NoCacheAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Thêm nhiều headers để chắc chắn ngăn cache
            context.HttpContext.Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate, private");
            context.HttpContext.Response.Headers.Add("Pragma", "no-cache");
            context.HttpContext.Response.Headers.Add("Expires", "-1");
            context.HttpContext.Response.Headers.Add("Last-Modified", DateTime.UtcNow.ToString("R"));
            context.HttpContext.Response.Headers.Add("ETag", Guid.NewGuid().ToString());

            base.OnActionExecuting(context);
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            // Đảm bảo headers được set sau khi action thực thi
            context.HttpContext.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate, private";
            context.HttpContext.Response.Headers["Pragma"] = "no-cache";
            context.HttpContext.Response.Headers["Expires"] = "-1";

            base.OnActionExecuted(context);
        }
    }
}
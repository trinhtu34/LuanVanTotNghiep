using System.Net;

namespace testpayment6._0.Helpers
{
    public static class NetworkHelper
    {
        /// <summary>
        /// Lấy địa chỉ IP của người dùng
        /// </summary>
        /// <param name="context">HttpContext</param>
        /// <returns>Địa chỉ IP dưới dạng chuỗi</returns>
        public static string GetIpAddress(HttpContext context)
        {
            string ipAddress;
            try
            {
                // Thử lấy IP từ các header thông dụng
                ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? context.Request.Headers["X-Real-IP"].FirstOrDefault();

                // Nếu không có header, lấy từ kết nối trực tiếp
                if (string.IsNullOrEmpty(ipAddress))
                {
                    ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                }

                // Nếu có nhiều IP trong X-Forwarded-For, lấy IP đầu tiên
                if (!string.IsNullOrEmpty(ipAddress) && ipAddress.Contains(","))
                {
                    ipAddress = ipAddress.Split(',')[0].Trim();
                }

                // Kiểm tra định dạng IPv6
                if (ipAddress == "::1")
                {
                    ipAddress = "127.0.0.1";
                }

                // Kiểm tra xem có phải là địa chỉ IP hợp lệ không
                if (!IPAddress.TryParse(ipAddress, out _))
                {
                    ipAddress = "127.0.0.1";
                }
            }
            catch
            {
                ipAddress = "127.0.0.1";
            }

            return ipAddress;
        }
    }
}
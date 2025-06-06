using Newtonsoft.Json;
using System;

namespace testpayment6._0.ResponseModels
{
    public class LoginResponse
    {
        [JsonProperty("userId")]
        public string UserId { get; set; }

        [JsonProperty("uPassword")]
        public string UPassword { get; set; }

        [JsonProperty("customerName")]
        public string CustomerName { get; set; }

        [JsonProperty("rolesId")]
        public int RolesId { get; set; }

        [JsonProperty("phoneNumber")]
        public string PhoneNumber { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("createAt")]
        public DateTime? CreateAt { get; set; }
    }
}

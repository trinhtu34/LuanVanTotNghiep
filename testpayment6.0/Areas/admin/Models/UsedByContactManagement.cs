using System.ComponentModel.DataAnnotations;

namespace testpayment6._0.Areas.admin.Models
{
    public class ContactFormModel
    {
        public int ContactId { get; set; }
        public string UserId { get; set; }
        public string Content { get; set; }
        public DateTime CreateAt { get; set; }
    }

    public class ContactFormViewModel
    {
        [Display(Name = "Từ ngày")]
        public DateTime? FromDate { get; set; }

        [Display(Name = "Đến ngày")]
        public DateTime? ToDate { get; set; }

        [Display(Name = "User ID")]
        public string UserId { get; set; }

        [Display(Name = "Tìm kiếm nội dung")]
        public string ContentSearch { get; set; }

        [Display(Name = "Loại tìm kiếm")]
        public string SearchType { get; set; } = "contains";

        [Display(Name = "Phân biệt chữ hoa/thường")]
        public bool CaseSensitive { get; set; }

        [Display(Name = "Sắp xếp theo")]
        public string SortBy { get; set; } = "createAt";

        [Display(Name = "Thứ tự")]
        public string SortOrder { get; set; } = "desc";

        [Display(Name = "Lọc nhanh")]
        public string QuickDateFilter { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }

        public List<ContactFormModel> ContactForms { get; set; } = new List<ContactFormModel>();

        // Helper properties
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
        public int PreviousPage => PageNumber - 1;
        public int NextPage => PageNumber + 1;

        public bool HasFilters => FromDate.HasValue || ToDate.HasValue ||
                                 !string.IsNullOrEmpty(UserId) ||
                                 !string.IsNullOrEmpty(ContentSearch);

        public List<(string Value, string Text)> SearchTypeOptions => new List<(string, string)>
        {
            ("contains", "Chứa"),
            ("exact", "Chính xác"),
            ("startswith", "Bắt đầu với"),
            ("endswith", "Kết thúc với")
        };

        public List<(string Value, string Text)> SortByOptions => new List<(string, string)>
        {
            ("createAt", "Ngày tạo"),
            ("userId", "User ID"),
            ("content", "Nội dung")
        };

        public List<(string Value, string Text)> SortOrderOptions => new List<(string, string)>
        {
            ("desc", "Giảm dần"),
            ("asc", "Tăng dần")
        };

        public List<(string Value, string Text)> QuickDateFilterOptions => new List<(string, string)>
        {
            ("", "Chọn khoảng thời gian"),
            ("today", "Hôm nay"),
            ("yesterday", "Hôm qua"),
            ("thisWeek", "Tuần này"),
            ("lastWeek", "Tuần trước"),
            ("thisMonth", "Tháng này"),
            ("lastMonth", "Tháng trước"),
            ("thisYear", "Năm này"),
            ("lastYear", "Năm trước")
        };

        public List<(int Value, string Text)> PageSizeOptions => new List<(int, string)>
        {
            (10, "10"),
            (25, "25"),
            (50, "50"),
            (100, "100")
        };
    }
}
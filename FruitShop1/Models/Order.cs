using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FruitShop1.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên khách hàng là bắt buộc.")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ là bắt buộc.")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "UserId là bắt buộc.")]
        public string UserId { get; set; } // Liên kết với ApplicationUser, bắt buộc

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; } // Navigation property

        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public List<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

        [Required]
        public string Status { get; set; } = "Chờ xác nhận"; // Mặc định tiếng Việt

        public string? RefundStatus { get; set; } = "None"; // Trạng thái hoàn trả

        public DateTime? EstimatedDeliveryDate { get; set; } // Thời gian dự kiến giao
    }
}
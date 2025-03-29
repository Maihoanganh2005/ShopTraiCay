using System.ComponentModel.DataAnnotations.Schema;

namespace FruitShop1.Models
{
    public class OrderDetail
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")] // Fix lỗi decimal
        public decimal Price { get; set; }

        public int Quantity { get; set; }

        [ForeignKey("OrderId")]
        public Order Order { get; set; }
    }
}

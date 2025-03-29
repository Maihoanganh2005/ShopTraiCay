namespace FruitShop1.Models
{
    public class CartItem
    {
        public int Id { get; set; } // Khóa chính
        public string UserId { get; set; } // Liên kết với Identity User
        public int FruitId { get; set; } // Liên kết với sản phẩm
        public required string Name { get; set; } // ✅ Đảm bảo Name không null
        public string? ImageUrl { get; set; } // Ảnh sản phẩm
        public decimal Price { get; set; } // Giá sản phẩm
        public int Quantity { get; set; } // Số lượng
        
        public Fruit Fruit { get; set; } // Navigation Property
    }
}

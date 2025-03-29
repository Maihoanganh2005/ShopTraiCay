using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace FruitShop1.Models
{
    public class Fruit
    {
        // Primary Key
        [Key]
        public int Id { get; set; }

        // Product name - required, max length 100 characters
        [Required, StringLength(100)]
        public required string Name { get; set; }

        // Price - required, decimal with 18 digits total, 2 after decimal point
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public required decimal Price { get; set; }

        // Discount price - optional, same decimal format
        [Column(TypeName = "decimal(18,2)")]
        public decimal? DiscountPrice { get; set; }

        // Description - optional, defaults to empty string
        public string Description { get; set; } = string.Empty;

        // Flash sale timing - both optional
        public DateTime? FlashSaleStart { get; set; }
        public DateTime? FlashSaleEnd { get; set; }

        // Image URL - optional
        public string? ImageUrl { get; set; }

        // Stock quantity - required, defaults to 10
        [Required]
        public int Stock { get; set; } = 10;

        // Foreign key to Category - required
        [Required]
        public required int CategoryId { get; set; }

        // Navigation property to Category entity
        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        // Purchase counter - defaults to 0
        public int PurchaseCount { get; set; } = 0;

        // One-to-many relationship with Review entities
        public List<Review> Reviews { get; set; } = new List<Review>();
    }
}
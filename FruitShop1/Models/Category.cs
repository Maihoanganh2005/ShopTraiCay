using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace FruitShop1.Models
{
    public class Category
    {
        [Key] // Khóa chính
        public int Id { get; set; }

        [Required, StringLength(100)] // Tên danh mục bắt buộc, tối đa 100 ký tự
        public required string Name { get; set; }

        public ICollection<Fruit> Fruits { get; set; } = new List<Fruit>(); // Danh sách trái cây thuộc danh mục
    }
}

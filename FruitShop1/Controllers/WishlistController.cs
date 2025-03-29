using FruitShop1.Data;
using FruitShop1.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace FruitShop1.Controllers
{
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string WishlistSessionKey = "Wishlist";

        public WishlistController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1️⃣ Hiển thị danh sách sản phẩm yêu thích
        [HttpGet]
        public IActionResult Index()
        {
            var wishlist = GetWishlistFromSession();
            Console.WriteLine($"Wishlist items: {wishlist.Count}"); // Debug
            foreach (var item in wishlist)
            {
                Console.WriteLine($"ID: {item.Id}, Name: {item.Name}, ImageUrl: {item.ImageUrl}");
            }
            return View(wishlist);
        }

        // 2️⃣ Thêm sản phẩm vào Wishlist
        [HttpGet]
        public IActionResult AddToWishlist(int id)
        {
            var product = _context.Fruits.Find(id);
            if (product == null) return NotFound();

            Console.WriteLine($"Adding to Wishlist - ID: {product.Id}, ImageUrl: {product.ImageUrl}"); // Debug

            var wishlist = GetWishlistFromSession();
            if (!wishlist.Any(p => p.Id == id))
            {
                wishlist.Add(product);
                SaveWishlistToSession(wishlist);
            }

            return RedirectToAction("Index");
        }

        // 3️⃣ Xóa sản phẩm khỏi Wishlist
        [HttpGet]
        public IActionResult RemoveFromWishlist(int id)
        {
            var wishlist = GetWishlistFromSession();
            wishlist.RemoveAll(p => p.Id == id);
            SaveWishlistToSession(wishlist);

            return RedirectToAction("Index");
        }

        // 🛠 Lấy danh sách wishlist từ Session
        private List<Fruit> GetWishlistFromSession()
        {
            var sessionData = HttpContext.Session.GetString(WishlistSessionKey);
            return sessionData != null ? JsonConvert.DeserializeObject<List<Fruit>>(sessionData) ?? new List<Fruit>() : new List<Fruit>();
        }

        // 🛠 Lưu danh sách wishlist vào Session
        private void SaveWishlistToSession(List<Fruit> wishlist)
        {
            HttpContext.Session.SetString(WishlistSessionKey, JsonConvert.SerializeObject(wishlist));
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FruitShop1.Data;
using FruitShop1.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FruitShop1.Controllers
{
    public class FruitController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FruitController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ✅ 1. Hiển thị danh sách sản phẩm (Chỉ Xem) với tìm kiếm, lọc, sắp xếp, phân trang
        public async Task<IActionResult> Index(string search, int? categoryId, decimal? minPrice, decimal? maxPrice, string sortOrder = "name", int page = 1)
        {
            var fruits = _context.Fruits.Include(f => f.Category).AsQueryable();

            // Log dữ liệu ban đầu
            var allFruits = await fruits.ToListAsync();
            Console.WriteLine($"Total fruits in database: {allFruits.Count}");
            foreach (var fruit in allFruits)
            {
                Console.WriteLine($"Fruit: ID={fruit.Id}, Name={fruit.Name}, CategoryId={fruit.CategoryId}, Price={fruit.Price}, Stock={fruit.Stock}");
            }

            // Tìm kiếm theo tên
            if (!string.IsNullOrEmpty(search))
            {
                fruits = fruits.Where(f => f.Name.Contains(search));
                Console.WriteLine($"After search filter (search={search}): {await fruits.CountAsync()} items");
            }

            // Lọc theo danh mục
            if (categoryId.HasValue)
            {
                fruits = fruits.Where(f => f.CategoryId == categoryId.Value);
                Console.WriteLine($"After category filter (categoryId={categoryId}): {await fruits.CountAsync()} items");
            }

            // Lọc theo khoảng giá
            if (minPrice.HasValue)
            {
                fruits = fruits.Where(f => f.Price >= minPrice.Value);
                Console.WriteLine($"After minPrice filter (minPrice={minPrice}): {await fruits.CountAsync()} items");
            }
            if (maxPrice.HasValue)
            {
                fruits = fruits.Where(f => f.Price <= maxPrice.Value);
                Console.WriteLine($"After maxPrice filter (maxPrice={maxPrice}): {await fruits.CountAsync()} items");
            }

            // Sắp xếp
            fruits = sortOrder.ToLower() switch
            {
                "price_asc" => fruits.OrderBy(f => f.Price),
                "price_desc" => fruits.OrderByDescending(f => f.Price),
                _ => fruits.OrderBy(f => f.Name)
            };

            int pageSize = 9;
            int totalItems = await fruits.CountAsync();
            var fruitList = await fruits.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            // Log kết quả cuối cùng
            Console.WriteLine($"Total items after all filters: {totalItems}");
            Console.WriteLine($"Items in current page: {fruitList.Count}");
            if (fruitList.Count > 0)
            {
                foreach (var fruit in fruitList)
                {
                    Console.WriteLine($"Displayed fruit: ID={fruit.Id}, Name={fruit.Name}, CategoryId={fruit.CategoryId}, Price={fruit.Price}, Stock={fruit.Stock}");
                }
            }
            else
            {
                Console.WriteLine("No fruits displayed after filtering.");
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.SortOrder = sortOrder;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            return View(fruitList);
        }

        // ✅ 2. Xem chi tiết sản phẩm (Chỉ Xem) với đánh giá và kiểm tra tồn kho
        public async Task<IActionResult> Details(int id)
        {
            var fruit = await _context.Fruits
                .Include(f => f.Category)
                .Include(f => f.Reviews) // Bao gồm đánh giá
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fruit == null) return NotFound();

            ViewBag.IsInStock = fruit.Stock > 0; // Kiểm tra còn hàng
            return View(fruit);
        }

        // ✅ 3. Thêm vào giỏ hàng (Yêu cầu đăng nhập) - Hoàn thiện với tính tổng giá
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddToCart(int fruitId, int quantity = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // 🔥 Lấy ID người dùng
            if (userId == null)
            {
                TempData["Error"] = "Bạn cần đăng nhập để thêm sản phẩm vào giỏ hàng.";
                return RedirectToAction("Login", "Account");
            }

            var fruit = await _context.Fruits.FindAsync(fruitId);
            if (fruit == null || fruit.Stock < quantity)
            {
                TempData["Error"] = "Sản phẩm không tồn tại hoặc hết hàng.";
                return RedirectToAction("Index");
            }

            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.FruitId == fruitId);

            if (cartItem == null)
            {
                cartItem = new CartItem
                {
                    UserId = userId,
                    Name = fruit.Name, // ✅ Gán giá trị Name từ sản phẩm
                    FruitId = fruitId,
                    Quantity = quantity
                };
                _context.CartItems.Add(cartItem);
            }
            else
            {
                cartItem.Quantity += quantity;
                if (cartItem.Quantity > fruit.Stock)
                {
                    TempData["Error"] = "Số lượng vượt quá tồn kho.";
                    return RedirectToAction("Index");
                }
                _context.CartItems.Update(cartItem);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"{fruit.Name} đã được thêm vào giỏ hàng!";
            return RedirectToAction("Index");
        }



        // ✅ 4. Đánh giá sản phẩm (Yêu cầu đăng nhập)
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddReview(int fruitId, int rating, string comment)
        {
            if (rating < 1 || rating > 5)
            {
                TempData["Error"] = "Điểm đánh giá phải từ 1 đến 5.";
                return RedirectToAction("Details", new { id = fruitId });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var fruit = await _context.Fruits.FindAsync(fruitId);

            if (fruit == null)
            {
                return NotFound();
            }

            var review = new Review
            {
                FruitId = fruitId,
                UserId = userId,
                Rating = rating,
                Comment = comment,
                CreatedDate = DateTime.Now
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đánh giá của bạn đã được gửi!";
            return RedirectToAction("Details", new { id = fruitId });
        }
    }
}
using FruitShop1.Data;
using FruitShop1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace FruitShop1.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminFruitController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public AdminFruitController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // ✅ Hiển thị danh sách sản phẩm
        public async Task<IActionResult> Index()
        {
            var fruits = await _context.Fruits.Include(f => f.Category).ToListAsync();
            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(fruits);
        }

        // ✅ Hiển thị form thêm sản phẩm
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View();
        }

        // ✅ Xử lý thêm sản phẩm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Fruit fruit, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                fruit.ImageUrl = await UploadImage(imageFile) ?? "default.jpg";
                _context.Fruits.Add(fruit);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm sản phẩm thành công!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(fruit);
        }

        // ✅ Hiển thị form chỉnh sửa sản phẩm
        public async Task<IActionResult> Edit(int id)
        {
            var fruit = await _context.Fruits.FindAsync(id);
            if (fruit == null) return NotFound();

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(fruit);
        }

        // ✅ Xử lý cập nhật sản phẩm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Fruit fruit, IFormFile? imageFile)
        {
            if (id != fruit.Id) return NotFound();

            var existingFruit = await _context.Fruits.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
            if (existingFruit == null) return NotFound();

            if (ModelState.IsValid)
            {
                fruit.ImageUrl = imageFile != null ? await UploadImage(imageFile) : existingFruit.ImageUrl;
                _context.Update(fruit);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật sản phẩm thành công!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(fruit);
        }

        // ✅ Hiển thị trang xác nhận xóa
        public async Task<IActionResult> Delete(int id)
        {
            var fruit = await _context.Fruits.Include(f => f.Category).FirstOrDefaultAsync(f => f.Id == id);
            if (fruit == null) return NotFound();

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(fruit);
        }

        // ✅ Xử lý xóa sản phẩm
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var fruit = await _context.Fruits.FindAsync(id);
            if (fruit == null) return NotFound();

            _context.Fruits.Remove(fruit);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Sản phẩm đã được xóa!";
            return RedirectToAction(nameof(Index));
        }

        // ✅ Hiển thị chi tiết sản phẩm
        public async Task<IActionResult> Details(int id)
        {
            var fruit = await _context.Fruits.Include(f => f.Category).FirstOrDefaultAsync(f => f.Id == id);
            if (fruit == null) return NotFound();

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(fruit);
        }

        // 🛠 Upload hình ảnh sản phẩm
        private async Task<string?> UploadImage(IFormFile? imageFile)
        {
            if (imageFile == null) return null;

            string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images");
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            return uniqueFileName;
        }
    }
}
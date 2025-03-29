using FruitShop1.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FruitShop1.Controllers
{
    [Authorize(Roles = "Admin")] // ✅ Chỉ Admin mới có quyền xem báo cáo
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1️⃣ 📈 **Báo cáo doanh thu theo tháng**
        public async Task<IActionResult> RevenueReport(int year)
        {
            if (year == 0) year = DateTime.Now.Year;

            var revenueData = await _context.Orders
                .Where(o => o.OrderDate.Year == year)
                .GroupBy(o => o.OrderDate.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    TotalRevenue = g.Sum(o => o.TotalAmount)
                })
                .OrderBy(g => g.Month)
                .ToListAsync();

            ViewBag.Year = year;
            return View(revenueData);
        }

        // 2️⃣ 📦 **Thống kê đơn hàng theo trạng thái**
        public async Task<IActionResult> OrderStatusReport()
        {
            var orderStats = await _context.Orders
                .GroupBy(o => o.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            return View(orderStats);
        }

        // 3️⃣ 🔥 **Biểu đồ sản phẩm bán chạy**
        public async Task<IActionResult> TopSellingProducts()
        {
            var topProducts = await _context.OrderDetails
                .GroupBy(od => od.ProductId)
                .Select(g => new
                {
                    ProductName = g.First().ProductName,
                    TotalSold = g.Sum(od => od.Quantity)
                })
                .OrderByDescending(g => g.TotalSold)
                .Take(5)
                .ToListAsync();

            return View(topProducts);
        }

        // 4️⃣ 📦 **Báo cáo tồn kho**
        public async Task<IActionResult> InventoryReport()
        {
            var inventoryData = await _context.Fruits
                .Select(f => new
                {
                    f.Name,
                    f.Stock
                })
                .OrderBy(f => f.Stock) // Sắp xếp sản phẩm có tồn kho thấp lên đầu
                .ToListAsync();

            return View(inventoryData);
        }
    }
}

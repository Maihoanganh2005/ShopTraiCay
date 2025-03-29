using System.Diagnostics;
using FruitShop1.Data;
using FruitShop1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FruitShop1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // ?? Ki?m tra d? li?u danh m?c
            ViewBag.Categories = await _context.Categories.ToListAsync();

            // ?? L?y 8 s?n ph?m m?i nh?t làm Featured
            ViewBag.FeaturedFruits = await _context.Fruits
                                        .OrderByDescending(f => f.Id)
                                        .Take(8)
                                        .ToListAsync();

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

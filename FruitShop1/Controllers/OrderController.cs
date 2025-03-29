using FruitShop1.Data;
using FruitShop1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace FruitShop1.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private const string CartSessionKey = "Cart";

        public OrderController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Xem danh sách đơn hàng của người dùng (GET)
        public async Task<IActionResult> MyOrders()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderDetails)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            return View(orders);
        }

        // Xem tất cả đơn hàng (Admin) (GET)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            return View(orders);
        }

        // Trang thanh toán (GET)
        public IActionResult Checkout()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = _userManager.FindByIdAsync(userId).Result;
            var cart = GetCartFromSession();
            if (!cart.Any())
            {
                TempData["Error"] = "Giỏ hàng trống! Vui lòng thêm sản phẩm trước khi thanh toán.";
                return RedirectToAction("Index", "Cart");
            }

            ViewBag.CustomerName = user?.UserName ?? User.Identity?.Name ?? "Khách";
            ViewBag.Email = user?.Email ?? "Chưa có email";
            ViewBag.RewardPoints = user?.RewardPoints ?? 0;
            ViewBag.CartItems = cart;
            ViewBag.TotalAmount = cart.Sum(c => c.Price * c.Quantity);
            return View();
        }

        // Xử lý thanh toán (POST) - Không cộng điểm, chỉ trừ nếu sử dụng
        [HttpPost]
        public async Task<IActionResult> Checkout(string customerName, string email, string address, decimal totalAmount)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = GetCartFromSession();
            if (!cart.Any())
            {
                TempData["Error"] = "Giỏ hàng trống!";
                return RedirectToAction("Index", "Cart");
            }

            var calculatedTotal = cart.Sum(c => c.Price * c.Quantity);
            var user = await _userManager.FindByIdAsync(userId);
            var rewardPoints = user?.RewardPoints ?? 0;
            var maxDiscount = Math.Min(rewardPoints, calculatedTotal);

            if (totalAmount < 0 || totalAmount > calculatedTotal || totalAmount < (calculatedTotal - maxDiscount))
            {
                TempData["Error"] = "Tổng tiền không hợp lệ!";
                ViewBag.CustomerName = customerName;
                ViewBag.Email = email;
                ViewBag.RewardPoints = rewardPoints;
                ViewBag.CartItems = cart;
                ViewBag.TotalAmount = calculatedTotal;
                return View();
            }

            if (string.IsNullOrEmpty(customerName) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(address))
            {
                TempData["Error"] = "Vui lòng điền đầy đủ thông tin!";
                ViewBag.CustomerName = customerName;
                ViewBag.Email = email;
                ViewBag.RewardPoints = rewardPoints;
                ViewBag.CartItems = cart;
                ViewBag.TotalAmount = calculatedTotal;
                return View();
            }

            var order = new Order
            {
                UserId = userId,
                CustomerName = customerName,
                Email = email,
                Address = address,
                OrderDate = DateTime.Now,
                TotalAmount = totalAmount,
                Status = "Chờ xác nhận",
                RefundStatus = "None",
                EstimatedDeliveryDate = DateTime.Now.AddDays(3),
                OrderDetails = cart.Select(item => new OrderDetail
                {
                    ProductId = item.FruitId,
                    ProductName = item.Name,
                    Price = item.Price,
                    Quantity = item.Quantity
                }).ToList()
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var item in cart)
            {
                var product = await _context.Fruits.FindAsync(item.FruitId);
                if (product != null && product.Stock >= item.Quantity)
                {
                    product.Stock -= item.Quantity;
                    product.PurchaseCount += item.Quantity;
                }
                else
                {
                    TempData["Error"] = $"Sản phẩm {product?.Name} không đủ hàng!";
                    _context.Orders.Remove(order);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Index", "Cart");
                }
            }

            // Trừ điểm thưởng nếu được sử dụng
            if (calculatedTotal > totalAmount && user != null)
            {
                var usedPoints = calculatedTotal - totalAmount;
                user.RewardPoints -= (int)usedPoints;
                await _userManager.UpdateAsync(user);
            }

            HttpContext.Session.Remove(CartSessionKey);
            TempData["Success"] = $"Đơn hàng #{order.Id} đặt thành công! Điểm thưởng sẽ được cộng khi đơn hàng được xác nhận.";
            return RedirectToAction("MyOrders");
        }

        // Cập nhật trạng thái đơn hàng (Admin) - Cộng điểm thưởng khi xác nhận
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int orderId, string status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                TempData["Error"] = "Đơn hàng không tồn tại.";
                return RedirectToAction("Index");
            }

            if (order.Status != "Chờ xác nhận" || status != "Đã xác nhận")
            {
                TempData["Error"] = "Đơn hàng này không thể xác nhận.";
                return RedirectToAction("Index");
            }

            order.Status = status;
            order.EstimatedDeliveryDate = DateTime.Now.AddDays(3);

            // Cộng điểm thưởng khi xác nhận đơn hàng
            var user = await _userManager.FindByIdAsync(order.UserId);
            if (user != null)
            {
                int pointsEarned = (int)(order.TotalAmount / 1000); // 1 điểm cho mỗi 1000 VNĐ
                user.RewardPoints += pointsEarned;
                await _userManager.UpdateAsync(user);
                TempData["Success"] = $"Đơn hàng #{order.Id} đã được xác nhận thành công! Người dùng nhận {pointsEarned} điểm thưởng.";
            }
            else
            {
                TempData["Success"] = $"Đơn hàng #{order.Id} đã được xác nhận thành công!";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // Xóa đơn hàng (Admin) - Không cần trừ điểm vì chỉ xóa khi chưa giao
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                TempData["Error"] = "Đơn hàng không tồn tại.";
                return RedirectToAction("Index");
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đơn hàng #{id} đã được xóa thành công!";
            return RedirectToAction("Index");
        }

        // Hủy đơn hàng - Trừ điểm nếu đã xác nhận
        [HttpPost]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng!";
                return RedirectToAction("MyOrders");
            }

            if (order.Status != "Chờ xác nhận" && order.Status != "Đang xử lý")
            {
                TempData["Error"] = "Không thể hủy đơn hàng đã được xử lý hoặc đã giao!";
                return RedirectToAction("MyOrders");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && order.Status == "Đã xác nhận") // Chỉ trừ điểm nếu đã xác nhận
            {
                int pointsEarned = (int)(order.TotalAmount / 1000);
                user.RewardPoints -= pointsEarned;
                await _userManager.UpdateAsync(user);
            }

            foreach (var detail in order.OrderDetails)
            {
                var product = await _context.Fruits.FindAsync(detail.ProductId);
                if (product != null)
                {
                    product.Stock += detail.Quantity;
                    product.PurchaseCount -= detail.Quantity;
                }
            }

            order.Status = "Đã hủy";
            order.RefundStatus = "Completed";
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đơn hàng #{order.Id} đã được hủy thành công!";
            return RedirectToAction("MyOrders");
        }

        // Yêu cầu hoàn trả - Trừ điểm nếu đơn đã giao
        [HttpPost]
        public async Task<IActionResult> RequestRefund(int orderId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng!";
                return RedirectToAction("MyOrders");
            }

            if (order.Status != "Đã giao" || order.RefundStatus != "None")
            {
                TempData["Error"] = "Không thể yêu cầu hoàn trả cho đơn hàng này!";
                return RedirectToAction("MyOrders");
            }

            order.RefundStatus = "Pending";

            // Trừ điểm thưởng đã cộng khi đơn hàng được xác nhận
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                int pointsEarned = (int)(order.TotalAmount / 1000);
                user.RewardPoints -= pointsEarned;
                await _userManager.UpdateAsync(user);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Yêu cầu hoàn trả cho đơn hàng #{order.Id} đã được gửi!";
            return RedirectToAction("MyOrders");
        }

        private List<CartItem> GetCartFromSession()
        {
            var sessionData = HttpContext.Session.GetString(CartSessionKey);
            return sessionData != null ? JsonConvert.DeserializeObject<List<CartItem>>(sessionData) ?? new List<CartItem>() : new List<CartItem>();
        }
    }
}
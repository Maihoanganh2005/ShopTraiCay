using FruitShop1.Data;
using FruitShop1.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace FruitShop1.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private const string CartSessionKey = "Cart";

        public CartController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1️⃣ Hiển thị giỏ hàng
        [HttpGet]
        public IActionResult Index()
        {
            var cart = GetCartFromSession();
            return View(cart);
        }

        // 2️⃣ Thêm sản phẩm vào giỏ hàng
        [HttpGet]
        public IActionResult AddToCart(int id)
        {
            var product = _context.Fruits.Find(id);
            if (product == null || product.Stock <= 0)
            {
                TempData["Error"] = "Sản phẩm không tồn tại hoặc đã hết hàng!";
                return RedirectToAction("Index");
            }

            var cart = GetCartFromSession();
            var cartItem = cart.FirstOrDefault(p => p.FruitId == id);

            if (cartItem != null)
            {
                if (product.Stock > cartItem.Quantity)
                {
                    cartItem.Quantity++;
                }
                else
                {
                    TempData["Error"] = $"Sản phẩm {product.Name} đã hết hàng!";
                }
            }
            else
            {
                // Giữ nguyên ImageUrl từ product, không sửa đổi
                cart.Add(new CartItem
                {
                    FruitId = product.Id,
                    Name = product.Name,
                    ImageUrl = product.ImageUrl, // Dùng trực tiếp giá trị từ database
                    Price = product.Price,
                    Quantity = 1,
                    UserId = User?.Identity?.IsAuthenticated == true ? _userManager.GetUserId(User) : "Guest"
                });
            }

            SaveCartToSession(cart);
            TempData["Success"] = $"Đã thêm {product.Name} vào giỏ hàng!";
            return RedirectToAction("Index");
        }

        // 3️⃣ Cập nhật số lượng sản phẩm trong giỏ hàng
        // Cập nhật số lượng (POST)
        [HttpPost]
        public IActionResult UpdateCart(int id, int quantity)
        {
            var cart = GetCartFromSession();
            var cartItem = cart.FirstOrDefault(p => p.FruitId == id);
            var product = _context.Fruits.Find(id);

            if (cartItem != null && product != null)
            {
                if (quantity > 0 && quantity <= product.Stock)
                {
                    cartItem.Quantity = quantity;
                    SaveCartToSession(cart);
                    return Json(new { success = true, total = cart.Sum(c => c.Price * c.Quantity) });
                }
                else if (quantity == 0)
                {
                    cart.Remove(cartItem);
                    SaveCartToSession(cart);
                    return Json(new { success = true, total = cart.Sum(c => c.Price * c.Quantity) });
                }
                else
                {
                    return Json(new { success = false, message = $"Sản phẩm {product.Name} chỉ còn {product.Stock} cái!" });
                }
            }
            return Json(new { success = false, message = "Không tìm thấy sản phẩm!" });
        }

        // 4️⃣ Xóa sản phẩm khỏi giỏ hàng
        [HttpGet]
        public IActionResult RemoveFromCart(int id)
        {
            var cart = GetCartFromSession();
            cart.RemoveAll(p => p.FruitId == id);
            SaveCartToSession(cart);
            return RedirectToAction("Index");
        }

        // 5️⃣ Trang thanh toán (GET)
        [HttpGet]
        public IActionResult Checkout()
        {
            var cart = GetCartFromSession();
            if (cart.Count == 0) return RedirectToAction("Index");

            var order = new Order
            {
                TotalAmount = cart.Sum(c => c.Price * c.Quantity) // Tính tổng tiền tạm thời để hiển thị
            };
            return View(order);
        }

        // 6️⃣ Xử lý thanh toán (POST)
        [HttpPost]
        public async Task<IActionResult> Checkout(Order order, bool useRewardPoints)
        {
            var cart = GetCartFromSession();
            if (cart.Count == 0) return RedirectToAction("Index");

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Vui lòng điền đầy đủ thông tin!";
                order.TotalAmount = cart.Sum(c => c.Price * c.Quantity); // Giữ tổng tiền để hiển thị lại form
                return View(order);
            }

            decimal discount = 0;
            if (useRewardPoints && user.RewardPoints > 0)
            {
                discount = user.RewardPoints;
                user.RewardPoints = 0;
            }

            var newOrder = new Order
            {
                UserId = user.Id,
                CustomerName = order.CustomerName,
                Email = order.Email,
                Address = order.Address,
                OrderDate = DateTime.Now,
                TotalAmount = cart.Sum(c => c.Price * c.Quantity) - discount,
                Status = "Chờ xác nhận",
                RefundStatus = "Pending"
            };

            _context.Orders.Add(newOrder);
            await _context.SaveChangesAsync();

            foreach (var item in cart)
            {
                var orderDetail = new OrderDetail
                {
                    OrderId = newOrder.Id,
                    ProductId = item.FruitId,
                    ProductName = item.Name,
                    Price = item.Price,
                    Quantity = item.Quantity
                };
                _context.OrderDetails.Add(orderDetail);

                var product = _context.Fruits.Find(item.FruitId);
                if (product != null)
                {
                    if (product.Stock >= item.Quantity)
                    {
                        product.Stock -= item.Quantity;
                        product.PurchaseCount += item.Quantity;
                    }
                    else
                    {
                        TempData["Error"] = $"Sản phẩm {product.Name} không đủ hàng!";
                        return RedirectToAction("Index");
                    }
                }
            }

            int pointsEarned = (int)(newOrder.TotalAmount * 0.01m);
            user.RewardPoints += pointsEarned;
            _context.Update(user);
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove(CartSessionKey);
            TempData["Success"] = $"Đơn hàng đã đặt thành công! Bạn nhận được {pointsEarned} điểm thưởng.";
            return RedirectToAction("OrderSuccess");
        }
        // 10️⃣ Xóa toàn bộ giỏ hàng (GET)
        [HttpGet]
        public IActionResult ClearCart()
        {
            var cart = GetCartFromSession();
            if (cart.Count == 0)
            {
                TempData["Error"] = "Giỏ hàng đã trống!";
            }
            else
            {
                cart.Clear();
                SaveCartToSession(cart);
                TempData["Success"] = "Giỏ hàng đã được xóa sạch!";
            }
            return RedirectToAction("Index");
        }

        // 11️⃣ Xóa toàn bộ giỏ hàng (POST - Bảo mật hơn)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ClearCartConfirmed()
        {
            var cart = GetCartFromSession();
            if (cart.Count == 0)
            {
                TempData["Error"] = "Giỏ hàng đã trống!";
            }
            else
            {
                cart.Clear();
                SaveCartToSession(cart);
                TempData["Success"] = "Giỏ hàng đã được xóa sạch!";
            }
            return RedirectToAction("Index");
        }
        // 7️⃣ Trang xác nhận đơn hàng thành công
        [HttpGet]
        public IActionResult OrderSuccess()
        {
            return View();
        }

        // 8️⃣ Hủy đơn hàng (chỉ khi chưa giao)
        [HttpPost]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            if (order.Status == "Pending" || order.Status == "Processing")
            {
                order.Status = "Cancelled";
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đơn hàng đã được hủy thành công!";
            }
            else
            {
                TempData["Error"] = "Bạn không thể hủy đơn hàng khi đã giao!";
            }

            return RedirectToAction("MyOrders");
        }

        // 9️⃣ Yêu cầu hoàn trả đơn hàng
        [HttpPost]
        public async Task<IActionResult> RequestRefund(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            if (order.Status == "Shipped")
            {
                order.RefundStatus = "Pending";
                await _context.SaveChangesAsync();
                TempData["Success"] = "Yêu cầu hoàn trả đã được gửi!";
            }
            else
            {
                TempData["Error"] = "Bạn chỉ có thể yêu cầu hoàn trả khi đơn hàng đã giao!";
            }

            return RedirectToAction("MyOrders");
        }

        // 🛠 Lấy danh sách giỏ hàng từ Session
        private List<CartItem> GetCartFromSession()
        {
            var sessionData = HttpContext.Session.GetString(CartSessionKey);
            return sessionData != null ? JsonConvert.DeserializeObject<List<CartItem>>(sessionData) ?? new List<CartItem>() : new List<CartItem>();
        }

        // 🛠 Lưu danh sách giỏ hàng vào Session
        private void SaveCartToSession(List<CartItem> cart)
        {
            HttpContext.Session.SetString(CartSessionKey, JsonConvert.SerializeObject(cart));
        }
    }
}
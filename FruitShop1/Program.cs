using FruitShop1.Data;
using FruitShop1.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1️⃣ Kết nối Database (SQL Server)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2️⃣ Cấu hình Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true; // ✅ Yêu cầu số
    options.Password.RequiredLength = 8; // ✅ Mật khẩu tối thiểu 8 ký tự
    options.Password.RequireNonAlphanumeric = true; // ✅ Yêu cầu ký tự đặc biệt
    options.Password.RequireUppercase = true; // ✅ Yêu cầu chữ hoa
    options.Password.RequireLowercase = true; // ✅ Yêu cầu chữ thường
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// 3️⃣ Thêm Session & Authentication
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // ✅ Tự động đăng xuất sau 30 phút
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // ✅ Cần cho Identity UI
builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/Account/AccessDenied"; // ✅ Khi bị chặn, chuyển hướng đến trang này
});
var app = builder.Build();

// 4️⃣ Tạo Role & Admin mặc định (chỉ chạy khi ứng dụng khởi động)
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    // ✅ Danh sách các vai trò cần tạo
    string[] roles = { "Admin", "User" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // ✅ Tạo tài khoản Admin nếu chưa có
    string adminEmail = "admin@fruitshop.com";
    string adminPassword = "Admin@123";

    if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}

// 5️⃣ Cấu hình Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication(); // ✅ Kích hoạt xác thực
app.UseAuthorization();
app.UseSession();

// ✅ Điều hướng Admin
app.MapGet("/admin", context =>
{
    context.Response.Redirect("/Admin/Dashboard");
    return Task.CompletedTask;
});

// 6️⃣ Định tuyến
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages(); // ✅ Hỗ trợ Identity UI

app.Run();

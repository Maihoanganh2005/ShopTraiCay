using Microsoft.AspNetCore.Identity;

namespace FruitShop1.Models
{
    public class ApplicationUser : IdentityUser
    {
        public int RewardPoints { get; set; } = 0; // Điểm thưởng mặc định là 0
    }
}

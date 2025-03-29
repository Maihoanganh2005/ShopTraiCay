namespace FruitShop1.Models
{
    public class Review
    {
        public int Id { get; set; }
        public int FruitId { get; set; }
        public string UserId { get; set; }
        public int Rating { get; set; } // 1-5 sao
        public string Comment { get; set; }
        public DateTime CreatedDate { get; set; }

        public Fruit Fruit { get; set; }
    }
}
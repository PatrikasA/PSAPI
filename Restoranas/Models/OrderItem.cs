namespace Restoranas.Models
{
    public class OrderItem
    {
        public string Name { get; set; }
        public int Count { get; set; }
        public double Price { get; set; }

        public int OrderId { get; set; }
    }
}

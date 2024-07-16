namespace AngularDemoAppAPIS.Models
{
    public class OrderItem
    {
        public int orderItemId { get; set; }
        public int restaurantId { get; set; }
        public int itemId { get; set; }
        public int quantity { get; set; }
        public decimal price { get; set; }
        public string itemName { get; set; }
    }
}

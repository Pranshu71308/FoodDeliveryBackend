namespace AngularDemoAppAPIS.Models
{
    public class OrderItemRequest
    {
        public int RestaurantId { get; set; }
        public int ItemId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}

namespace AngularDemoAppAPIS.Models
{
    public class CartModel
    {
        public int UserId { get; set; }
        public int RestaurantId { get; set; }
        public int ItemId { get; set; }
        public int Quantity { get; set; }
        public decimal totalPrice { get; set; }
    }
}

namespace AngularDemoAppAPIS.Models
{
    public class UserOrder
    {
        public int orderId { get; set; }
        public string transactionNumber { get; set; }
        public decimal totalPrice { get; set; }
        public string date { get; set; }
        public string time { get; set; }
        public string address { get; set; }
        public string restaurantName { get; set; }
        public byte[] image { get; set; }
        public List<OrderItem> items { get; set; }
    }
}

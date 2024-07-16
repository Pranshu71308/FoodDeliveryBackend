namespace AngularDemoAppAPIS.Models
{
    public class OrderDataModel
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public string TransactionNumber { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime DateTime { get; set; }
        public string Address { get; set; }
        public List<OrderItemData> Items { get; set; } = new List<OrderItemData>();
    }
}

namespace AngularDemoAppAPIS.Models
{
    public class OrderRequest
    {
        public int UserId { get; set; }
        public string TransactionNumber { get; set; }
        public string Address { get; set; }
        public decimal TotalPrice { get; set; }

        public List<OrderItemRequest> Items { get; set; }

    }
}

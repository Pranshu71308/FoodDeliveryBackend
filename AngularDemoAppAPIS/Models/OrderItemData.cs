namespace AngularDemoAppAPIS.Models
{
    public class OrderItemData
    {
        public int OrderItemId { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public decimal MenuItemPrice { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public byte[] MenuFoodImage { get; set; }
        public int Quantity { get; set; }
        public decimal OrderItemPrice { get; set; }
    }
}

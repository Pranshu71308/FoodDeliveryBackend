namespace AngularDemoAppAPIS.Models
{
    public class CartDetailsModel
    {
        public int CartId { get; set; }
        public int UserId { get; set; }
        public int RestaurantId { get; set; }
        public int ItemId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string ItemName { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public byte[] MenuFoodImage { get; set; }

        public bool is_deleted { get; set; }
    }
}

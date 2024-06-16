namespace AngularDemoAppAPIS.Models
{
    public class FoodMenuModel
    {
        public int itemId { get; set; }
        public string ?itemName { get; set; }
        public decimal price { get; set; }
        public string ?description { get; set; }
        public string ?category { get; set; }
        public byte[] ?FoodImage { get; set; } // Image data as byte array

    }
}

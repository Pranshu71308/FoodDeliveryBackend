namespace AngularDemoAppAPIS.Models
{
    public class MenuItemModel
    {
        public string itemName { get; set; }
        public decimal price { get; set; }
        public string description { get; set; }
        public string category { get; set; }
        public byte[]? image { get; set; }
    }
}

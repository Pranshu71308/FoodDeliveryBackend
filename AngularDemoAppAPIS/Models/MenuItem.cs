namespace AngularDemoAppAPIS.Models
{
    public class MenuItem
    {
        public int ItemId { get; set; }
        public string? ItemName { get; set; }
        public decimal? Price { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public byte[]? Image { get; set; }
    }
}

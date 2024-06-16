namespace AngularDemoAppAPIS.Models
{
    public class RestaurantData
    {
        public int RestaurantID { get; set; }
        public string? RestaurantName { get; set; }
        public string? Location { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public float? Ratings { get; set; }
        public byte[] Image { get; set; } // Image data as byte array

    }
}

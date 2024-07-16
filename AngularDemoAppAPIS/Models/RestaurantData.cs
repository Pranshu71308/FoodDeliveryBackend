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
        public byte[] Image { get; set; } 

        public TimeSpan? openingTime { get; set; } 
        public TimeSpan? closingTime { get; set; }  
        public bool isClosed { get; set; } 
        public DateTime? closedStartDate { get; set; } 
        public DateTime? closedEndDate { get; set; }

        public int availableSeats { get; set; }

    }
}

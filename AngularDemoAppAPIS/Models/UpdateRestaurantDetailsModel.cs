namespace AngularDemoAppAPIS.Models
{
    public class UpdateRestaurantDetailsModel
    {
        public int? RestaurantID { get; set; }
        public string RestaurantName { get; set; }
        public string Location { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public double? Ratings { get; set; }
        public byte[]? Image { get; set; }
        public TimeSpan? OpeningTime { get; set; }
        public TimeSpan? ClosingTime { get; set; }
        public bool? IsClosed { get; set; }
        public DateTime? closedStartDate { get; set; }
        public DateTime? closedEndDate { get; set; }
        public int? AvailableTables { get; set; }
    }
}

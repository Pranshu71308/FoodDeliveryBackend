namespace AngularDemoAppAPIS.Models
{
    public class AddRestaurantRequestModel
    {
        public string RestaurantName { get; set; }
        public string Location { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public double Ratings { get; set; }
        public byte[]? ResImage { get; set; }
        public TimeSpan Opening { get; set; }
        public TimeSpan Closing { get; set; }
        public bool IsClosed { get; set; }
        public DateTime? CloseStartDate { get; set; }
        public DateTime? CloseEndDate { get; set; }
        public int AvailableTables { get; set; }
        public int BookingTimings { get; set; }
        public List<MenuItemModel> MenuItems { get; set; }
        public int UserId { get; set; } // New field for UserId

    }
}

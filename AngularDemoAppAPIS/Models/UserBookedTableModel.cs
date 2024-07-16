namespace AngularDemoAppAPIS.Models
{
    public class UserBookedTableModel
    {
        public int UserBookedTableId { get; set; }
        public int UserID { get; set; }
        public int RestaurantID { get; set; }
        public int Guests { get; set; }
        public DateOnly BookingDate { get; set; }
        public TimeSpan BookingTime { get; set; }
        //public string BookingTime { get; set; }
        public int OrderID { get; set; }
        public string OrderNumber { get; set; }
        public decimal Amount { get; set; }
        public bool IsDeleted { get; set; }

        public string? SpecialRequest { get; set; }

        public string? RestaurantName { get; set; }
        public string? Location { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public double? Ratings { get; set; }
        public byte[]? Image { get; set; }
        public TimeSpan? Opening { get; set; }
        public TimeSpan? Closing { get; set; }
        public bool? IsClosed { get; set; }
        public DateTime? CloseStartDate { get; set; }
        public DateTime? CloseEndDate { get; set; }
        public int? AvailableTables { get; set; }
    }
}

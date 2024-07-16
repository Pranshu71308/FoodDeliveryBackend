namespace AngularDemoAppAPIS.Models
{
    public class BookingDetails
    {
            public int UserBookedTableId { get; set; }
            public int UserId { get; set; }
            public int RestaurantId { get; set; }
            public int Guests { get; set; }
            public DateTime BookingDate { get; set; }
            public TimeSpan BookingTime { get; set; }
            public int? OrderId { get; set; }
            public string OrderNumber { get; set; }
            public decimal Amount { get; set; }
            public bool IsDeleted { get; set; }
            public string SpecialRequest { get; set; }
            public string Username { get; set; }
            public string Email { get; set; }
            public string PhoneNumber { get; set; }

    }
}

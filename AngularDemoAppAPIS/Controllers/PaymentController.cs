using AngularDemoAppAPIS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Npgsql;
using Razorpay.Api;
using Twilio.Types;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace AngularDemoAppAPIS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly NpgsqlConnection _connection;
        private readonly JwtOption _options;
        private readonly string _key = "rzp_test_AekOZ0gOtU4sda";
        private readonly string _secret = "2xJKtCqnyXecXvF5m5UKRHcK";
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _cache;
        public PaymentController(NpgsqlConnection connection, IOptions<JwtOption> options, IEmailService emailService)
        {
            _connection = connection;
            _connection.Open();
            _options = options.Value;
            _emailService = emailService;

        }
        [HttpPost("createOrder")]
        public async Task<IActionResult> CreateOrder([FromBody] OrderRequest request)
        {
            try
            {
                var restaurantIds = request.Items.Select(i => i.RestaurantId).ToArray();
                var itemIds = request.Items.Select(i => i.ItemId).ToArray();
                var quantities = request.Items.Select(i => i.Quantity).ToArray();
                var prices = request.Items.Select(i => i.Price).ToArray();

                using (var cmd = new NpgsqlCommand("SELECT insert_order(@p_userid, @p_transactionnumber, @p_totalprice, @p_address, @p_restaurantids, @p_item_ids, @p_quantities, @p_prices)", _connection))
                {
                    cmd.Parameters.AddWithValue("@p_userid", request.UserId);
                    cmd.Parameters.AddWithValue("@p_transactionnumber", request.TransactionNumber);
                    cmd.Parameters.AddWithValue("@p_totalprice", request.TotalPrice);
                    cmd.Parameters.AddWithValue("@p_address", request.Address);
                    cmd.Parameters.AddWithValue("@p_restaurantids", NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Integer, restaurantIds);
                    cmd.Parameters.AddWithValue("@p_item_ids", NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Integer, itemIds);
                    cmd.Parameters.AddWithValue("@p_quantities", NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Integer, quantities);
                    cmd.Parameters.AddWithValue("@p_prices", NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Numeric, prices);

                    await cmd.ExecuteNonQueryAsync();
                }
                SendMailAsync(request.UserId, request.TransactionNumber, request.Address);
                return Ok(new { message = "Orders created successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return BadRequest(new { error = ex.Message });
            }
        }

        private async Task SendMailAsync(int userId, string TransactionNumber, string Address)
        {
            using (var cmd = new NpgsqlCommand("SELECT email, phonenumber FROM get_user_details(@userId)", _connection))
            {
                cmd.Parameters.AddWithValue("userId", userId);

                try
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            //var email = result.ToString();
                            var email = reader["email"].ToString();
                            var phoneNumber = reader["phonenumber"].ToString();
                            var subject = "Order Confirmation";
                            var imageUrl = "https://marketplace.canva.com/EAEtwkoOhsA/1/0/400w/canva-yellow-and-black-fun-modern-restaurant-food-logo-2SZkH1XCBNc.jpg";
                            var body = "<p>Your order has been placed successfully.</p>" +
                                "<p> Your Transaction number is = " + TransactionNumber +
                                "</p><p>Order will be delivered at " + Address +
                                 $"<br><br><p><img src='{imageUrl}' alt='Order Image' style='width:100px;height:auto;'/></p>";

                            await _emailService.SendEmailAsync(email, subject, body);
                            const string accountSid = "ACd2d822bb512f3926ac82853a55113adf";
                            const string authToken = "6d03b7a3021c789b285a6aef525bb18d";
                            TwilioClient.Init(accountSid, authToken);

                            var message = await MessageResource.CreateAsync(
                                from: new PhoneNumber($"whatsapp:{+14155238886}"),
                                to: new PhoneNumber($"whatsapp:+91{phoneNumber}"),
                                body: $"Your order has been placed successfully. Your Transaction number is: {TransactionNumber}. Order will be delivered at: {Address}."
                            );

                        }
                        else
                        {
                            Console.WriteLine("No email found for the given user ID.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Internal server error: {ex.Message}");
                }
            }
        }

        [HttpGet("GetUserBookings")]
        public async Task<IActionResult> GetUserBookingsAsync(int userId)
        {
            var bookings = new List<UserBookedTableModel>();

            try
            {

                using (var cmd = new NpgsqlCommand("SELECT * FROM FetchUserBookings(@userId)", _connection))
                {
                    cmd.Parameters.AddWithValue("userId", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            bookings.Add(new UserBookedTableModel
                            {
                                UserBookedTableId = reader.GetInt32(0),
                                UserID = reader.GetInt32(1),
                                RestaurantID = reader.GetInt32(2),
                                Guests = reader.GetInt32(3),
                                BookingDate = DateOnly.FromDateTime(reader.GetDateTime(4)),
                                BookingTime = reader.GetTimeSpan(5),
                                OrderID = reader.GetInt32(6),
                                OrderNumber = reader.GetString(7),
                                Amount = reader.GetDecimal(8),
                                IsDeleted = reader.GetBoolean(9),
                                SpecialRequest = reader.IsDBNull(10) ? null : reader.GetString(10),
                                RestaurantName = reader.GetString(11),
                                Location = reader.GetString(12),
                                Latitude = reader.GetDecimal(13),
                                Longitude = reader.GetDecimal(14),
                                Ratings = reader.GetDouble(15),
                                Image = reader["Image"] as byte[],
                                Opening = reader.GetTimeSpan(17),
                                Closing = reader.GetTimeSpan(18),
                                IsClosed = reader.GetBoolean(19),

                            });
                        }


                    }

                }

                return Ok(bookings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("InsertOrUpdateBooking")]
        public async Task<IActionResult> InsertOrUpdateBookingAsync([FromBody] UserBookedTableModel bookingModel)
        {
            if (bookingModel == null)
            {
                return BadRequest("Invalid booking data.");
            }

            try
            {

                using (var cmd = new NpgsqlCommand("SELECT InsertOrUpdateBooking(@userID, @restaurantID, @guests, @bookingDate, @bookingTime, @orderID, @orderNumber, @amount,@SpecialRequest)", _connection))
                {
                    cmd.Parameters.AddWithValue("userID", bookingModel.UserID);
                    cmd.Parameters.AddWithValue("restaurantID", bookingModel.RestaurantID);
                    cmd.Parameters.AddWithValue("guests", bookingModel.Guests);
                    cmd.Parameters.AddWithValue("bookingDate", bookingModel.BookingDate);
                    cmd.Parameters.AddWithValue("bookingTime", NpgsqlTypes.NpgsqlDbType.Time, bookingModel.BookingTime);
                    cmd.Parameters.AddWithValue("orderID", bookingModel.OrderID);
                    cmd.Parameters.AddWithValue("orderNumber", NpgsqlTypes.NpgsqlDbType.Varchar, bookingModel.OrderNumber);
                    cmd.Parameters.AddWithValue("amount", NpgsqlTypes.NpgsqlDbType.Numeric, bookingModel.Amount);
                    cmd.Parameters.AddWithValue("SpecialRequest", NpgsqlTypes.NpgsqlDbType.Varchar, bookingModel.SpecialRequest!);

                    await cmd.ExecuteNonQueryAsync();
                }
                await SendOrderConfirmation(bookingModel.UserID, bookingModel.OrderNumber, bookingModel.SpecialRequest!, bookingModel.RestaurantID);


                return Ok(new { message = "Booking inserted/updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        private async Task SendOrderConfirmation(int userId, string transactionNumber, string SpecialRequest, int RestaurantID)
        {
            string restaurantName = string.Empty;
            string email = string.Empty;
            string phoneNumber = string.Empty;

            // Fetch user details
            using (var cmd = new NpgsqlCommand("SELECT email, phonenumber FROM get_user_details(@userId)", _connection))
            {
                cmd.Parameters.AddWithValue("userId", userId);

                try
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            email = reader["email"].ToString();
                            phoneNumber = reader["phonenumber"].ToString();
                        }
                        else
                        {
                            Console.WriteLine("No details found for the given user ID.");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Internal server error: {ex.Message}");
                    return;
                }
            }

            // Fetch restaurant name
            using (var restaurantCmd = new NpgsqlCommand("SELECT restaurantname FROM restaurantsdata WHERE restaurantid = @restaurantId", _connection))
            {
                restaurantCmd.Parameters.AddWithValue("restaurantId", RestaurantID);

                try
                {
                    var restaurantResult = await restaurantCmd.ExecuteScalarAsync();
                    if (restaurantResult != null)
                    {
                        restaurantName = restaurantResult.ToString();
                    }
                    else
                    {
                        restaurantName = "Unknown Restaurant";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Internal server error: {ex.Message}");
                    return;
                }
            }

            var subject = "Booking Confirmation";
            var imageUrl = "https://marketplace.canva.com/EAEtwkoOhsA/1/0/400w/canva-yellow-and-black-fun-modern-restaurant-food-logo-2SZkH1XCBNc.jpg";
            var body = "<p>Your Booking has been confirmed for "+restaurantName+".</p>" +
                       "<p> Your Transaction number is = " + transactionNumber +
                       $"<br><br><p><img src='{imageUrl}' alt='Order Image' style='width:100px;height:auto;'/></p>" +
                       "Restaurant will try to " + SpecialRequest + "of yours, However if not fulfilled there will be no refund of cover charge.";

            await _emailService.SendEmailAsync(email, subject, body);

            const string accountSid = "ACd2d822bb512f3926ac82853a55113adf";
            const string authToken = "6d03b7a3021c789b285a6aef525bb18d";
            TwilioClient.Init(accountSid, authToken);

            var message = await MessageResource.CreateAsync(
                from: new PhoneNumber("whatsapp:+14155238886"),
                to: new PhoneNumber($"whatsapp:+91{phoneNumber}"),
                body: $"Your Booking has been confirmed at {restaurantName}. Your Transaction number is: {transactionNumber}." +
                $" Restaurant will try to {SpecialRequest}, However if not fulfilled there will be no refund of cover charge."
            );

            Console.WriteLine($"WhatsApp message sent: {message.Sid}");
        }

        [HttpPost("DeleteBooking")]
        public IActionResult DeleteBooking(int userId, int restaurantId)
        {
            try
            {

                using (var cmd = new NpgsqlCommand("SELECT DeleteBooking(@userID, @restaurantID)", _connection))
                {
                    cmd.Parameters.AddWithValue("userID", userId);
                    cmd.Parameters.AddWithValue("restaurantID", restaurantId);

                    cmd.ExecuteNonQuery();
                }
                return Ok(new { message = "Booking deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

    }
}

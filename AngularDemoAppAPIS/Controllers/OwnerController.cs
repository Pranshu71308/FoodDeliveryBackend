using AngularDemoAppAPIS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;
using System.Data;

namespace AngularDemoAppAPIS.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class OwnerController : ControllerBase
    {
        private readonly NpgsqlConnection _connection;
        private readonly JwtOption _options;
        public OwnerController(NpgsqlConnection connection, IOptions<JwtOption> options)
        {
            _connection = connection;
            _connection.Open();
            _options = options.Value;
        }
        [HttpGet("GetRestaurantsByUserId/{userId}")]
        public ActionResult<List<RestaurantData>> GetRestaurantsByUserId(int userId)
        {
            var RestaurantData1 = new List<RestaurantData>();

            using (var cmd = new NpgsqlCommand("SELECT * FROM public.get_restaurants_by_userid(@userid)", _connection))
            {
                cmd.Parameters.AddWithValue("userid", userId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {

                        var RestaurantDetails = new RestaurantData
                        {
                            RestaurantID = reader.GetInt32(0),
                            RestaurantName = reader.GetString(1),
                            Location = reader.GetString(2),
                            Latitude = reader.GetDecimal(3),
                            Longitude = reader.GetDecimal(4),
                            Ratings = reader.GetFloat(5),
                            Image = reader["Image"] as byte[],
                            openingTime = reader.GetTimeSpan(7),
                            closingTime = reader.GetTimeSpan(8),
                            isClosed = reader.IsDBNull(9) ? false : reader.GetBoolean(9),
                            closedStartDate = reader.IsDBNull(10) ? null : reader.GetDateTime(reader.GetOrdinal("closestartdate")),
                            closedEndDate = reader.IsDBNull(11) ? null : reader.GetDateTime(reader.GetOrdinal("closeenddate")),
                            availableSeats = reader.IsDBNull(12) ? 0 : reader.GetInt32(12)


                        };
                        RestaurantData1.Add(RestaurantDetails);
                    }
                }
            }
            return Ok(RestaurantData1);
        }


        [HttpGet("GetRestaurantsById/{restaurantId}")]
        public ActionResult<List<RestaurantData>> GetRestaurantsById(int restaurantId)
        {
            var RestaurantDetails = new RestaurantData { };

            using (var cmd = new NpgsqlCommand("SELECT * FROM public.get_restaurants_by_id(@restaurantId)", _connection))
            {
                cmd.Parameters.AddWithValue("restaurantId", restaurantId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {

                        RestaurantDetails = new RestaurantData
                        {
                            RestaurantID = restaurantId,
                            RestaurantName = reader.GetString(0),
                            Location = reader.GetString(1),
                            Latitude = reader.GetDecimal(2),
                            Longitude = reader.GetDecimal(3),
                            Ratings = reader.GetFloat(4),
                            Image = reader["Image"] as byte[],
                            openingTime = reader.GetTimeSpan(6),
                            closingTime = reader.GetTimeSpan(7),
                            isClosed = reader.IsDBNull(8) ? false : reader.GetBoolean(8),
                            closedStartDate = reader.IsDBNull(9) ? null : reader.GetDateTime(reader.GetOrdinal("closestartdate")),
                            closedEndDate = reader.IsDBNull(10) ? null : reader.GetDateTime(reader.GetOrdinal("closeenddate")),
                            availableSeats = reader.IsDBNull(11) ? 0 : reader.GetInt32(11)


                        };
                        //RestaurantData1.Add(RestaurantDetails);
                    }
                }
            }
            return Ok(RestaurantDetails);
        }


        [HttpGet("GetOrdersByRestaurantId/{restaurantId}")]
        public ActionResult<List<OrderDataModel>> GetOrdersByRestaurantId(int restaurantId)
        {
            var orderDataDict = new Dictionary<int, OrderDataModel>();

            using (var cmd = new NpgsqlCommand("SELECT * FROM public.fetch_orders_by_restaurant(@restaurantId)", _connection))
            {
                cmd.Parameters.AddWithValue("restaurantId", restaurantId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int orderId = reader.GetInt32(reader.GetOrdinal("orderid"));

                        if (!orderDataDict.TryGetValue(orderId, out var orderData))
                        {
                            orderData = new OrderDataModel
                            {
                                OrderId = orderId,
                                UserId = reader.GetInt32(reader.GetOrdinal("userid")),
                                TransactionNumber = reader.GetString(reader.GetOrdinal("transactionnumber")),
                                TotalPrice = reader.GetDecimal(reader.GetOrdinal("totalprice")),
                                DateTime = reader.GetDateTime(reader.GetOrdinal("datetime")),
                                Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString(reader.GetOrdinal("address")),
                                Items = new List<OrderItemData>()
                            };
                            orderDataDict[orderId] = orderData;
                        }
                        var orderItem = new OrderItemData
                        {
                            OrderItemId = reader.GetInt32(reader.GetOrdinal("order_item_id")),
                            ItemName = reader.GetString(reader.GetOrdinal("item_name")),
                            MenuItemPrice = reader.GetDecimal(reader.GetOrdinal("menu_item_price")),
                            Description = reader.GetString(reader.GetOrdinal("description")),
                            Category = reader.GetString(reader.GetOrdinal("category")),
                            Quantity = reader.GetInt32(reader.GetOrdinal("quantity")),
                            OrderItemPrice = reader.GetDecimal(reader.GetOrdinal("order_item_price"))
                        };

                        orderData.Items.Add(orderItem);
                    }
                }
            }

            return Ok(orderDataDict.Values.ToList());
        }
        [HttpPost("AddRestaurantWithMenu")]
        public IActionResult AddRestaurantWithMenu([FromBody] AddRestaurantRequestModel request)
        {
            try
            {
                var menuItemsJson = JsonConvert.SerializeObject(request.MenuItems.Select(item => new
                {
                    item.itemName,
                    item.price,
                    item.description,
                    item.category,
                    menuFoodImage = item.image
                }));

                using (var cmd = new NpgsqlCommand("SELECT public.add_restaurant_and_create_menu_table(" +
                                                   "@restaurantname, @location, @latitude, @longitude, " +
                                                   "@ratings, @image, @opening, @closing, @isclosed, " +
                                                   "@closestartdate, @closeenddate, @availabletables, " +
                                                   "@bookingtimings, @menu_items, @userid)", _connection))
                {
                    cmd.Parameters.AddWithValue("restaurantname", NpgsqlDbType.Text, request.RestaurantName);
                    cmd.Parameters.AddWithValue("location", NpgsqlDbType.Text, request.Location);
                    cmd.Parameters.AddWithValue("latitude", NpgsqlDbType.Numeric, request.Latitude);
                    cmd.Parameters.AddWithValue("longitude", NpgsqlDbType.Numeric, request.Longitude);
                    cmd.Parameters.AddWithValue("ratings", NpgsqlDbType.Double, request.Ratings);

                    if (request.ResImage != null)
                    {
                        cmd.Parameters.AddWithValue("image", NpgsqlDbType.Bytea, request.ResImage);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("image", DBNull.Value);
                    }

                    cmd.Parameters.AddWithValue("opening", NpgsqlDbType.Time, request.Opening);
                    cmd.Parameters.AddWithValue("closing", NpgsqlDbType.Time, request.Closing);
                    cmd.Parameters.AddWithValue("isclosed", NpgsqlDbType.Boolean, request.IsClosed);

                    if (request.CloseStartDate.HasValue)
                    {
                        cmd.Parameters.AddWithValue("closestartdate", NpgsqlDbType.Timestamp, request.CloseStartDate);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("closestartdate", DBNull.Value);
                    }

                    if (request.CloseEndDate.HasValue)
                    {
                        cmd.Parameters.AddWithValue("closeenddate", NpgsqlDbType.Timestamp, request.CloseEndDate);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("closeenddate", DBNull.Value);
                    }

                    cmd.Parameters.AddWithValue("availabletables", NpgsqlDbType.Integer, request.AvailableTables);
                    cmd.Parameters.AddWithValue("bookingtimings", NpgsqlDbType.Integer, request.BookingTimings);
                    cmd.Parameters.AddWithValue("menu_items", NpgsqlDbType.Jsonb, menuItemsJson);
                    cmd.Parameters.AddWithValue("userid", NpgsqlDbType.Integer, request.UserId);

                    cmd.ExecuteNonQuery();
                }



                return Ok("Restaurant and menu added successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("UpdateRestaurantMenu")]
        public IActionResult UpdateRestaurantMenu([FromBody] UpdateRestaurantMenuModel request)
        {
            try
            {
                var menuItemsJson = JsonConvert.SerializeObject(request.MenuItems.Select(item => new
                {
                    item_id = item.ItemId,
                    item_name = item.ItemName,
                    price = item.Price,
                    description = item.Description,
                    category=item.Category,
                    menu_food_image = item.Image
                }));


                using (var cmd = new NpgsqlCommand("SELECT update_restaurant_menu(@restaurantid, @menu_items)", _connection))
                {
                    cmd.Parameters.AddWithValue("restaurantid", NpgsqlDbType.Integer, request.RestaurantId);
                    cmd.Parameters.AddWithValue("menu_items", NpgsqlDbType.Jsonb, menuItemsJson);
                    cmd.ExecuteNonQuery();
                    return Ok("Menu items updated successfully");

                 
                }

            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpGet("GetBookingsByRestaurant/{restaurantId}")]
        public ActionResult<List<BookingDetails>> GetBookingsByRestaurant(int restaurantId)
        {
            var bookingDetailsList = new List<BookingDetails>();

            using (var cmd = new NpgsqlCommand("SELECT * FROM get_bookings_by_restaurant(@p_restaurantid)", _connection))
            {
                cmd.Parameters.AddWithValue("p_restaurantid", restaurantId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var bookingDetails = new BookingDetails
                        {
                            UserBookedTableId = reader.GetInt32(0),
                            UserId = reader.GetInt32(1),
                            RestaurantId = reader.GetInt32(2),
                            Guests = reader.GetInt32(3),
                            BookingDate = reader.GetDateTime(4),
                            BookingTime = reader.GetTimeSpan(5),
                            OrderId = reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6),
                            OrderNumber = reader.IsDBNull(7) ? null : reader.GetString(7),
                            Amount = reader.GetDecimal(8),
                            IsDeleted = reader.GetBoolean(9),
                            SpecialRequest = reader.IsDBNull(10) ? null : reader.GetString(10),
                            Username = reader.GetString(11),
                            Email = reader.GetString(12),
                            PhoneNumber = reader.GetString(13)
                        };

                        bookingDetailsList.Add(bookingDetails);
                    }
                }
            }

            return Ok(bookingDetailsList);
        }

    }
}

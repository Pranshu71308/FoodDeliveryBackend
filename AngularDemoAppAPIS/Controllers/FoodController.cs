using AngularDemoAppAPIS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Npgsql;
using Org.BouncyCastle.Bcpg;
using System;
using static System.Net.Mime.MediaTypeNames;

namespace AngularDemoAppAPIS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class FoodController : ControllerBase
    {
        private readonly NpgsqlConnection _connection;
        private readonly JwtOption _options;

        public FoodController(NpgsqlConnection connection, IOptions<JwtOption> options)
        {
            _connection = connection;
            _connection.Open();
            _options = options.Value;
        }

        //[Authorize]
        [HttpGet("Restaurants")]
        public IActionResult RestauransData()
        {
            var RestaurantData1 = new List<RestaurantData>();

            using (var cmd = new NpgsqlCommand("SELECT * FROM public.RestaurantsData()", _connection))
            {
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
                            availableSeats = reader.IsDBNull(12)? 0 : reader.GetInt32(12)


                        };
                        RestaurantData1.Add(RestaurantDetails);
                    }
                }
            }
            return Ok(RestaurantData1);
        }

       
        [HttpPost("GetRestaurantMenu/{restaurantId}")]
        public IActionResult GetRestaurantMenu(int restaurantId)
        {
            var menudata = new List<FoodMenuModel>();

            using (var cmd = new NpgsqlCommand("SELECT * FROM fetch_restaurant_menu(@restaurantId)", _connection))
            {
                cmd.Parameters.AddWithValue("restaurantId", restaurantId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var menuDetails = new FoodMenuModel
                        {
                            itemId = reader.GetInt32(0),
                            itemName = reader.GetString(1),
                            price = reader.GetDecimal(2),
                            description = reader.IsDBNull(3) ? null : reader.GetString(3),
                            category = reader.IsDBNull(4) ? null : reader.GetString(4),
                            FoodImage = reader["menu_food_image"] as byte[]

                        };
                        menudata.Add(menuDetails);
                    }
                }
                return Ok(menudata);
            }
        }
        [Authorize]
        [HttpPost("StoreCartDetails")]
        public IActionResult StoreCartDetails([FromBody] CartModel cartModel)
        {
            if (cartModel == null)
            {
                return BadRequest("Invalid cart data.");
            }

            try
            {
                using (var cmd = new NpgsqlCommand("SELECT upsert_cart(@userId, @restaurantId, @itemId, @quantity, @price)", _connection))
                {
                    cmd.Parameters.AddWithValue("userId", cartModel.UserId);
                    cmd.Parameters.AddWithValue("restaurantId", cartModel.RestaurantId);
                    cmd.Parameters.AddWithValue("itemId", cartModel.ItemId);
                    cmd.Parameters.AddWithValue("quantity", cartModel.Quantity);
                    cmd.Parameters.AddWithValue("price", cartModel.totalPrice);

                    cmd.ExecuteNonQuery();
                }

                return Ok(new { message = "Item added to cart successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
            finally
            {
                _connection.Close();
            }
        }
        [Authorize]
        [HttpGet("GetCartDetails/{userId}")]
        public IActionResult GetCartDetails(int userId)
        {
            var cartDetails = new List<CartDetailsModel>();

            try
            {
                using (var cmd = new NpgsqlCommand("SELECT * FROM fetch_cart_details_with_item_name(@userId)", _connection))
                {
                    cmd.Parameters.AddWithValue("userId", userId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var cartDetail = new CartDetailsModel
                            {
                                CartId = reader.GetInt32(0),
                                UserId = reader.GetInt32(1),
                                RestaurantId = reader.GetInt32(2),
                                ItemId = reader.GetInt32(3),
                                Quantity = reader.GetInt32(4),
                                Price = reader.GetDecimal(5),
                                ItemName = reader.GetString(6),
                                Description = reader.IsDBNull(7) ? null : reader.GetString(7),
                                Category = reader.IsDBNull(8) ? null : reader.GetString(8),
                                MenuFoodImage = reader.IsDBNull(9) ? null : (byte[])reader["menu_food_image"],
                            };
                            cartDetails.Add(cartDetail);
                        }
                    }
                }

                return Ok(cartDetails);
            }
            catch (Exception ex)
            {
                _connection.Close();
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
            finally
            {
                _connection.Close();
            }
        }

        [HttpGet("GetUserOrders/{userId}")]
        public IActionResult GetUserOrders(int userId)
        {
            try
            {

                var ordersDictionary = new Dictionary<int, UserOrder>();

                using (var cmd = new NpgsqlCommand("SELECT * FROM get_user_orders(@userId)", _connection))
                {
                    cmd.Parameters.AddWithValue("userId", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var orderId = reader.GetInt32(0);

                            if (!ordersDictionary.ContainsKey(orderId))
                            {
                                var datetime = reader.GetDateTime(3);

                                var order = new UserOrder
                                {
                                    orderId = orderId,
                                    transactionNumber = reader.GetString(1),
                                    totalPrice = reader.GetDecimal(2),
                                    date = datetime.ToString("yyyy-MM-dd"),
                                    time = datetime.ToString("hh:mm tt"),
                                    address = reader.IsDBNull(4) ? null : reader.GetString(4),
                                    restaurantName = reader.IsDBNull(7) ? null : reader.GetString(7),
                                    image = reader["Image"] as byte[],
                                    items = new List<OrderItem>()
                                };

                                ordersDictionary.Add(orderId, order);
                            }

                            var item = new OrderItem
                            {
                                orderItemId = reader.GetInt32(5),
                                restaurantId = reader.GetInt32(6),
                                itemId = reader.GetInt32(9),
                                quantity = reader.GetInt32(10),
                                price = reader.GetDecimal(11),
                                itemName = reader.IsDBNull(12) ? null : reader.GetString(12)
                            };

                            ordersDictionary[orderId].items.Add(item);
                        }
                    }
                }

                return Ok(ordersDictionary.Values);

            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [Authorize]
        [HttpPost("DeleteCartDetails/{userId}")]
        public IActionResult DeleteCartDetails(int userId)
        {
            try
            {
                using (var cmd = new NpgsqlCommand("SELECT delete_cart_entries_for_user(@userId)", _connection))
                {
                    cmd.Parameters.AddWithValue("userId", userId);
                    cmd.ExecuteNonQuery();
                }
                return Ok(new { message = "Cart entries deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("InsertBookmark")]
        public async Task<IActionResult> InsertBookmark([FromBody] BookMarkModel request)
        {
            try
            {

                using (var cmd = new NpgsqlCommand("SELECT add_bookmark(@p_userid, @p_restaurantid)", _connection))
                {
                    cmd.Parameters.AddWithValue("p_userid", request.userId);
                    cmd.Parameters.AddWithValue("p_restaurantid", request.restaurantId);

                    await cmd.ExecuteNonQueryAsync();

                }
                return Ok(new { message = "Bookmark added successfully" });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", details = ex.Message });
            }
        }

        [HttpGet("GetBookmarkedRestaurants/{userid}")]
        public async Task<IActionResult> GetBookmarkedRestaurants(int userid)
        {
            try
            {
                var restaurantDetails = new List<RestaurantData>();


                var cmd = new NpgsqlCommand("SELECT * FROM get_bookmarked_restaurants(@p_userid)", _connection);
                cmd.Parameters.AddWithValue("p_userid", userid);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var detail = new RestaurantData
                    {
                        RestaurantID = reader.GetInt32(0),
                        RestaurantName = reader.GetString(1),
                        Location = reader.GetString(2),
                        Latitude = reader.GetDecimal(3),
                        Longitude = reader.GetDecimal(4),
                        Ratings = reader.GetFloat(5),
                        Image = reader["Image"] as byte[],
                        //openingTime = reader.GetTimeSpan(7),
                        //closingTime = reader.GetTimeSpan(8),
                        //isClosed = reader.GetBoolean(9),
                        //isClosed = reader.IsDBNull(10) ? (DateTime?)null : reader.GetDateTime(10),
                        //closedEndDate = reader.IsDBNull(11) ? (DateTime?)null : reader.GetDateTime(11),
                    };
                    restaurantDetails.Add(detail);
                }

                return Ok(restaurantDetails);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", details = ex.Message });
            }
        }
        [HttpPost("ToggleBookmark")]
        public IActionResult ToggleBookmark([FromBody] BookMarkModel model)
        {
            try
            {

                using (var cmd = new NpgsqlCommand("SELECT toggle_bookmark(@p_userId, @p_restaurantId)", _connection))
                {
                    cmd.Parameters.AddWithValue("@p_userId", model.userId);
                    cmd.Parameters.AddWithValue("@p_restaurantId", model.restaurantId);

                    cmd.ExecuteNonQuery();
                }

                return Ok(new { message = "Bookmark toggled successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while toggling bookmark", message = ex.Message });
            }
        }

        [HttpPost("UpdateRatings")]
        public IActionResult UpdateRatings([FromBody] RatingsModel model)
        {
            try
            {

                using (var cmd = new NpgsqlCommand("SELECT  public.update_restaurant_rating(@p_restaurantId, @p_new_ratings)", _connection))
                {
                    cmd.Parameters.AddWithValue("@p_restaurantId", model.restaurantid);
                    cmd.Parameters.AddWithValue("@p_new_ratings", model.ratings);

                    cmd.ExecuteNonQuery();
                }

                return Ok(new { message = "Ratings Updated" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while toggling bookmark", message = ex.Message });
            }
        }

    }
}



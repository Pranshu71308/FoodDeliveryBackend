using AngularDemoAppAPIS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Npgsql;
using Org.BouncyCastle.Bcpg;

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

        [Authorize]
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
                            Image = reader["Image"] as byte[]

                        };
                        RestaurantData1.Add(RestaurantDetails);
                    }
                }
            }
            return Ok(RestaurantData1);
        }

        [Authorize]
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
    }
}


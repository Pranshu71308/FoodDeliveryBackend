using AngularDemoAppAPIS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Npgsql;

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

            using (var cmd = new NpgsqlCommand("select * from RestaurantsData", _connection))
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
        [HttpGet("GetFoodData")]
        public IActionResult GetFoodData()
        {
            var fooddata = new List<FoodDataModel>();

            using (var cmd = new NpgsqlCommand("select * from users", _connection))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var foodDetails = new FoodDataModel
                        {
                            foodName = reader.GetString(1)
                        };
                        fooddata.Add(foodDetails);
                    }
                }
                return Ok(fooddata);
            }
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
    }
}


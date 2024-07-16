using AngularDemoAppAPIS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using NpgsqlTypes;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AngularDemoAppAPIS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly NpgsqlConnection _connection;
        private readonly JwtOption _options;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _cache;

        public AuthController(NpgsqlConnection connection, IOptions<JwtOption> options, IEmailService emailService, IMemoryCache cache)
        {
            _connection = connection;
            _connection.Open();
            _options = options.Value;
            _emailService = emailService;
            _cache = cache;

        }

        [HttpPost("Register")]
        public IActionResult CreateUser([FromBody] AuthenticationModel Authmodel)
        {
            if (Authmodel.Password != Authmodel.ConfirmPassword)
            {
                return BadRequest("Password and Confirm Password do not match.");
            }
            else
            {
                using (var cmd = new NpgsqlCommand("select public.create_user(@p_username,@p_email,@p_password,@p_phone_number)", _connection))
                {
                    cmd.Parameters.AddWithValue("p_username", Authmodel.Username!);
                    cmd.Parameters.AddWithValue("p_email", Authmodel.Email!);
                    cmd.Parameters.AddWithValue("p_password", Authmodel.Password!);
                    cmd.Parameters.AddWithValue("p_phone_number", Authmodel.PhoneNumber!);
                    try
                    {
                        cmd.ExecuteNonQuery();
                        return Ok(new { message = "User created successfully" });
                    }
                    catch (Exception ex)
                    {
                        return StatusCode(500, $"Internal server error: {ex.Message}");
                    }
                }
            }
        }
        [HttpPost("login")]
        public IActionResult LoginUser([FromBody] LoginModel loginModel)
        {
            try
            {
                using (var cmd = new NpgsqlCommand("SELECT userid, username FROM login_user(@p_identifier, @p_password)", _connection))
                {
                    cmd.Parameters.AddWithValue("p_identifier", loginModel.Identifier!);
                    cmd.Parameters.AddWithValue("p_password", loginModel.Password!);
                    var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        var userid = Convert.ToInt32(reader["userid"]);
                        var foundUsername = Convert.ToString(reader["username"]);

                        var jwtkey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_options.key));
                        var credentials = new SigningCredentials(jwtkey, SecurityAlgorithms.HmacSha256);
                        var claims = new List<Claim>
                        {
                            new Claim("userid", userid.ToString()),
                            new Claim("username", foundUsername!)
                        };
                        var sToken = new JwtSecurityToken(_options.key, _options.Issuer, claims: claims, expires: DateTime.Now.AddMinutes(120), signingCredentials: credentials);
                        var token = new JwtSecurityTokenHandler().WriteToken(sToken);
                        return Ok(new { token = token });
                    }
                    else
                    {
                        return Unauthorized("Invalid username/email or password.");
                    }
                }
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


        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordModel model)
        {
            using (var cmd = new NpgsqlCommand("SELECT email FROM users WHERE email = @p_email", _connection))
            {
                cmd.Parameters.AddWithValue("p_email", model.Email!);

                try
                {
                    var result = cmd.ExecuteScalar();
                    if (result != DBNull.Value && result != null)
                    {

                        string otp = GenerateOtp();
                        _cache.Set(model.Email!, otp, TimeSpan.FromMinutes(10));

                        await _emailService.SendEmailAsync(model.Email, "Password Reset OTP", $"Your OTP is: {otp}");

                        return Ok(new { message = "OTP sent to your email." });

                    }
                    else
                    {
                        return BadRequest("Enter correct email address.");
                    }
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Internal server error: {ex.Message}");
                }
            }
        }
        [HttpPost("verify-otp")]
        public IActionResult VerifyOtp([FromBody] VerifyOtpModel model)
        {
            try
            {
                if (_cache.TryGetValue(model.Email, out string storedOtp))
                {
                    if (storedOtp == model.Otp)
                    {
                        _cache.Remove(model.Email);

                        return Ok(new { message = "OTP verified successfully." });
                    }
                    else
                    {
                        return Unauthorized("Invalid OTP.");
                    }
                }
                else
                {
                    return BadRequest("No OTP found for the provided email.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpPost("reset-password")]
        public IActionResult ResetPassword([FromBody] ResetPasswordModel model)
        {

            using (var cmd = new NpgsqlCommand("UPDATE users SET password = @p_password WHERE email = @p_email", _connection))
            {
                cmd.Parameters.AddWithValue("p_password", model.NewPassword!);
                cmd.Parameters.AddWithValue("p_email", model.Email!);

                try
                {
                    cmd.ExecuteNonQuery();
                    return Ok(new { message = "Password reset successfully." });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Internal server error: {ex.Message}");
                }
            }
        }

        private string GenerateOtp()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        [Authorize]
        [HttpGet("GetAllUsers")]
        public IActionResult GetAllUsers()
        {
            var userdata = new List<AuthenticationModel>();

            using (var cmd = new NpgsqlCommand("select * from public.UsersData()", _connection))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var userDetails = new AuthenticationModel
                        {
                            Username = reader.GetString(1),
                            Email = reader.GetString(2),
                            Password = reader.GetString(3),
                            PhoneNumber = reader.GetString(4)
                        };
                        userdata.Add(userDetails);
                    }
                }
            }
            return Ok(userdata);
        }

        [HttpGet("GetUserDetails/{userid}")]
        public IActionResult GetUserDetails(int userid)
        {
            var userDetail = new UserDetailsModel();

            using (var cmd = new NpgsqlCommand("SELECT * FROM get_user_details(@p_userid)", _connection))
            {
                cmd.Parameters.AddWithValue("p_userid", userid);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        userDetail.userId = reader.GetInt32(0);
                        userDetail.userName = reader.GetString(1);
                        userDetail.email = reader.GetString(2);
                        userDetail.phoneNumber = reader.IsDBNull(3) ? null : reader.GetString(3);
                        userDetail.address1 = reader.IsDBNull(4) ? null : reader.GetString(4);
                        userDetail.address2 = reader.IsDBNull(5) ? null : reader.GetString(5);
                        userDetail.userAuthorityId = reader.IsDBNull(6) ? 2 : reader.GetInt32(6);
                    }
                    else
                    {
                        return NotFound();
                    }
                }
            }
            return Ok(userDetail);
        }
        [HttpPost("UpdateUserDetails")]
        public IActionResult UpdateUserDetails([FromBody] UpdateUserDetailsModel model)
        {
            if (model == null || model.UserId <= 0)
            {
                return BadRequest("Invalid user details.");
            }

            using (var cmd = new NpgsqlCommand("SELECT update_user_details(@p_userid, @p_username, @p_email, @p_password, @p_phonenumber, @p_userauthorityid)", _connection))
            {
                cmd.Parameters.AddWithValue("p_userid", model.UserId);
                cmd.Parameters.AddWithValue("p_username", String.IsNullOrWhiteSpace(model.Username) ? (object)DBNull.Value : model.Username);
                cmd.Parameters.AddWithValue("p_email", String.IsNullOrWhiteSpace(model.Email) ? (object)DBNull.Value : model.Email);
                cmd.Parameters.AddWithValue("p_password", String.IsNullOrWhiteSpace(model.Password) ? (object)DBNull.Value : model.Password);
                cmd.Parameters.AddWithValue("p_phonenumber", String.IsNullOrWhiteSpace(model.Phonenumber) ? (object)DBNull.Value : model.Phonenumber);
                cmd.Parameters.AddWithValue("p_userauthorityid", model.UserAuthorityId.HasValue ? (object)model.UserAuthorityId : DBNull.Value);

                try
                {
                    cmd.ExecuteNonQuery();
                    return Ok("User details updated successfully.");
                }
                catch (Exception ex)
                {
                    return StatusCode(500, "Internal server error: " + ex.Message);
                }
            }
        }
        [HttpPost("UpdateRestaurantDetails")]
        public IActionResult UpdateRestaurantDetails([FromBody] UpdateRestaurantDetailsModel model)
        {
            if (model == null || model.RestaurantID == null || model.RestaurantID <= 0)
            {
                return BadRequest("Invalid restaurant details.");
            }

            using (var cmd = new NpgsqlCommand("SELECT public.update_restaurant_details(@p_restaurantid, @p_restaurantname, @p_location, @p_latitude, @p_longitude, @p_ratings, @p_image, @p_opening, @p_closing, @p_isclosed, @p_closestartdate, @p_closeenddate, @p_availabletables)", _connection))
            {
                cmd.Parameters.AddWithValue("p_restaurantid", model.RestaurantID.Value);
                cmd.Parameters.AddWithValue("p_restaurantname", string.IsNullOrWhiteSpace(model.RestaurantName) ? (object)DBNull.Value : model.RestaurantName);
                cmd.Parameters.AddWithValue("p_location", string.IsNullOrWhiteSpace(model.Location) ? (object)DBNull.Value : model.Location);
                cmd.Parameters.AddWithValue("p_latitude", model.Latitude.HasValue ? (object)model.Latitude.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("p_longitude", model.Longitude.HasValue ? (object)model.Longitude.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("p_ratings", model.Ratings.HasValue ? (object)model.Ratings.Value : DBNull.Value);

                var imageParameter = cmd.Parameters.Add("p_image", NpgsqlTypes.NpgsqlDbType.Bytea);
                imageParameter.Value = model.Image ?? (object)new byte[0];

                var openingTimeParameter = cmd.Parameters.Add("p_opening", NpgsqlTypes.NpgsqlDbType.Time);
                openingTimeParameter.Value = model.OpeningTime.HasValue ? (object)model.OpeningTime.Value : DBNull.Value;

                var closingTimeParameter = cmd.Parameters.Add("p_closing", NpgsqlTypes.NpgsqlDbType.Time);
                closingTimeParameter.Value = model.ClosingTime.HasValue ? (object)model.ClosingTime.Value : DBNull.Value;

                cmd.Parameters.AddWithValue("p_isclosed", model.IsClosed.HasValue ? (object)model.IsClosed.Value : DBNull.Value);
                cmd.Parameters.Add("p_closestartdate", NpgsqlDbType.Timestamp).Value = (object)model.closedStartDate ?? DBNull.Value;
                cmd.Parameters.Add("p_closeenddate", NpgsqlDbType.Timestamp).Value = (object)model.closedEndDate ?? DBNull.Value;


                cmd.Parameters.AddWithValue("p_availabletables", model.AvailableTables.HasValue ? (object)model.AvailableTables.Value : DBNull.Value);

                try
                {
                    cmd.ExecuteNonQuery();
                    return Ok("Restaurant details updated successfully.");
                }
                catch (Exception ex)
                {
                    return StatusCode(500, "Internal server error: " + ex.Message);
                }
            }
        }
    }
}
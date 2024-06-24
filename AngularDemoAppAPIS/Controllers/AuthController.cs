using AngularDemoAppAPIS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
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
                        var sToken = new JwtSecurityToken(_options.key, _options.Issuer, claims: claims, expires: DateTime.Now.AddMinutes(20), signingCredentials: credentials);
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

                        // Send OTP via email
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
    }

}


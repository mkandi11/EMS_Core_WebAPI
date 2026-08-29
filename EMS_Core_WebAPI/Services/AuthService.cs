using EMS_Core_WebAPI.Helper;
using EMS_Core_WebAPI.Models;
using EMS_Core_WebAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace EMS_Framework_WebAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly DBConnection _db;
        private readonly IConfiguration _configuration;


        public AuthService(DBConnection db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        public async Task<LoginResponse?> LoginAsync(Login dto, CancellationToken cancellationToken = default)
        {
            const string sql = "SELECT * FROM [User]";
            var rows = await _db.ExecuteReaderAsync(
                sql,
                reader => new User
                {
                    UserId = reader.GetInt32(0),
                    Username = reader.GetValue("Username").ToString(),
                    Role = reader.GetValue("Role").ToString(),
                    PasswordHash = reader.GetValue("PasswordHash").ToString()
                },
                parameters: null,
                commandType: CommandType.Text,
                cancellationToken: cancellationToken
                ).ConfigureAwait(false);

            var user = rows.FirstOrDefault(u => u.Username == dto.Username);

            if (user == null)
            {
                return null;
            }

            var validPassword =
                BCrypt.Net.BCrypt.Verify(
                    dto.Password,
                    user.PasswordHash
                );

            if (!validPassword)
            {
                return null;
            }

            var token = GenerateToken(user);

            return new LoginResponse
            {
                Token = token,
                UserId = user.UserId,
                Username = user.Username,
                Role = user.Role
            };
        }

        private string GenerateToken(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");

            var key =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(
                        jwtSettings["Key"]!
                    )
                );

            var credentials =
                new SigningCredentials(
                    key,
                    SecurityAlgorithms.HmacSha256
                );

            var claims = new[]
            {

                new Claim(ClaimTypes.NameIdentifier,user.UserId.ToString()),

                new Claim(ClaimTypes.Name,user.Username),

                new Claim(ClaimTypes.Role,user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],

                audience: jwtSettings["Audience"],

                claims: claims,

                expires:
                    DateTime.UtcNow.AddMinutes(
                        double.Parse(
                            jwtSettings["ExpiryMinutes"]!
                        )
                    ),

                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PasswordController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public PasswordController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPatch]
    public IActionResult ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
        {
            return Unauthorized("Идентификатор пользователя отсутствует.");
        }

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return BadRequest("Некорректный идентификатор пользователя.");
        }

        using var connection = new SqliteConnection(_configuration.GetConnectionString("DefaultConnection"));
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT PasswordHash FROM Users WHERE Id = @userId";
        command.Parameters.AddWithValue("@userId", userId);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            var currentPasswordHash = reader.GetString(0);

            if (VerifyPassword(request.CurrentPassword, currentPasswordHash))
            {
                var newPasswordHash = HashPassword(request.NewPassword);
                var newToken = GenerateJwtToken(userId.ToString());

                command = connection.CreateCommand();
                command.CommandText = "UPDATE Users SET PasswordHash = @newPasswordHash, Token = @newToken WHERE Id = @userId";
                command.Parameters.AddWithValue("@newPasswordHash", newPasswordHash);
                command.Parameters.AddWithValue("@newToken", newToken);
                command.Parameters.AddWithValue("@userId", userId);
                command.ExecuteNonQuery();

                return Ok(new { Token = newToken });
            }
        }

        return BadRequest("Неверный текущий пароль.");
    }

    private string GenerateJwtToken(string userId)
    {
        var key = _configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(key))
        {
            throw new InvalidOperationException("Ключ JWT не настроен в appsettings.json.");
        }

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            expires: DateTime.Now.AddMinutes(_configuration.GetValue<int>("Jwt:ExpiryInMinutes")),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    private bool VerifyPassword(string password, string storedHash)
    {
        var hashOfInput = HashPassword(password);
        return hashOfInput == storedHash;
    }
}
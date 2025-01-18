using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] UserRegistrationRequest request)
    {
        var passwordHash = HashPassword(request.Password);

        using var connection = new SqliteConnection(_configuration.GetConnectionString("DefaultConnection"));
        connection.Open();

        var checkUserCommand = connection.CreateCommand();
        checkUserCommand.CommandText = "SELECT COUNT(*) FROM Users WHERE Username = @username";
        checkUserCommand.Parameters.AddWithValue("@username", request.Username);

        var userExistsResult = checkUserCommand.ExecuteScalar() as long?;
        var userExists = userExistsResult.HasValue && userExistsResult > 0;

        if (userExists)
        {
            return Conflict("Пользователь с таким именем уже существует.");
        }

        var insertUserCommand = connection.CreateCommand();
        insertUserCommand.CommandText = "INSERT INTO Users (Username, PasswordHash) VALUES (@username, @passwordHash); SELECT last_insert_rowid();";
        insertUserCommand.Parameters.AddWithValue("@username", request.Username);
        insertUserCommand.Parameters.AddWithValue("@passwordHash", passwordHash);

        var userId = insertUserCommand.ExecuteScalar() as long?;
        if (userId == null)
        {
            return StatusCode(500, "Ошибка при создании пользователя.");
        }

        var token = GenerateJwtToken(request.Username, (int)userId);

        var updateTokenCommand = connection.CreateCommand();
        updateTokenCommand.CommandText = "UPDATE Users SET Token = @token WHERE Id = @userId";
        updateTokenCommand.Parameters.AddWithValue("@token", token);
        updateTokenCommand.Parameters.AddWithValue("@userId", userId);
        updateTokenCommand.ExecuteNonQuery();

        return Ok(new { Token = token });
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] UserLoginRequest request)
    {
        using var connection = new SqliteConnection(_configuration.GetConnectionString("DefaultConnection"));
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Username, PasswordHash FROM Users WHERE Username = @username";
        command.Parameters.AddWithValue("@username", request.Username);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            var user = new User
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1),
                PasswordHash = reader.GetString(2),
                Token = null
            };

            if (VerifyPassword(request.Password, user.PasswordHash))
            {
                var token = GenerateJwtToken(user.Username, user.Id);

                var updateTokenCommand = connection.CreateCommand();
                updateTokenCommand.CommandText = "UPDATE Users SET Token = @token WHERE Id = @userId";
                updateTokenCommand.Parameters.AddWithValue("@token", token);
                updateTokenCommand.Parameters.AddWithValue("@userId", user.Id);
                updateTokenCommand.ExecuteNonQuery();

                return Ok(new { Token = token });
            }
        }

        return Unauthorized("Неверное имя пользователя или пароль.");
    }

    private string GenerateJwtToken(string username, int userId)
    {
        var key = _configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(key))
        {
            throw new InvalidOperationException("Ключ JWT не настроен в appsettings.json.");
        }

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.Now.AddMinutes(_configuration.GetValue<int>("Jwt:ExpiryInMinutes")),
            signingCredentials: creds
        );

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